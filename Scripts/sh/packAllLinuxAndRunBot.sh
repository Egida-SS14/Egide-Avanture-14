#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════
# SS14 Server + Egide.Bot Wrapper
# Сервер — якорь. Бот в отдельной папке инстанса.
# ═══════════════════════════════════════════════════════════════

# --- Пути ---
WRAPPER_DIR="$(cd "$(dirname "$0")" && pwd)"   # instances/egide/
BOT_DIR="${WRAPPER_DIR}/bot"                    # instances/egide/bot/
BOT_BINARY="${BOT_DIR}/Egide.Bot"
BOT_CONFIG="${BOT_DIR}/config.yml"

# Путь до исходников репозитория (настрой под себя!)
# Вариант 1: исходники рядом с instances/
# REPO_ROOT="${WRAPPER_DIR}/../../"
# Вариант 2: абсолютный путь
REPO_ROOT="/opt/ss14/Egide-Avanture-14"

BOT_PROJECT="${REPO_ROOT}/Egide.Bot"
BOT_PUBLISH_SRC="${BOT_PROJECT}/bin/Release/net8.0/linux-x64/publish/Egide.Bot"

BOT_RESTART_DELAY=5
SHUTDOWN=0
BOT_PID=""
SERVER_PID=""
BOT_LOOP_PID=""

# ═══════════════════════════════════════════════════════════════
# Graceful shutdown
# ═══════════════════════════════════════════════════════════════
cleanup() {
    local sig=$1
    echo "[Wrapper] Получен сигнал ${sig}. Останавливаем процессы..."
    SHUTDOWN=1

    if [ -n "$BOT_LOOP_PID" ] && kill -0 "$BOT_LOOP_PID" 2>/dev/null; then
        kill "$BOT_LOOP_PID" 2>/dev/null
        wait "$BOT_LOOP_PID" 2>/dev/null
    fi

    if [ -n "$BOT_PID" ] && kill -0 "$BOT_PID" 2>/dev/null; then
        echo "[Wrapper] Останавливаем бота (PID: $BOT_PID)..."
        kill -TERM "$BOT_PID" 2>/dev/null
        wait "$BOT_PID" 2>/dev/null
    fi

    if [ -n "$SERVER_PID" ] && kill -0 "$SERVER_PID" 2>/dev/null; then
        echo "[Wrapper] Останавливаем сервер (PID: $SERVER_PID)..."
        kill -TERM "$SERVER_PID" 2>/dev/null
        wait "$SERVER_PID" 2>/dev/null
    fi

    echo "[Wrapper] Завершено."
    exit 0
}

trap 'cleanup SIGTERM' SIGTERM
trap 'cleanup SIGINT' SIGINT

# ═══════════════════════════════════════════════════════════════
# Шаг 1: Сборка сервера и бота
# ═══════════════════════════════════════════════════════════════
cd "$REPO_ROOT" || exit 1

echo "[Wrapper] Обновление субмодулей..."
git submodule update --init --recursive

echo "[Wrapper] Сборка сервера..."
dotnet build -c Release

echo "[Wrapper] Публикация Egide.Bot..."
dotnet publish "${BOT_PROJECT}" -c Release -r linux-x64 --self-contained

# ═══════════════════════════════════════════════════════════════
# Шаг 2: Копируем бота в instances/egide/bot/ (вне bin/!)
# ═══════════════════════════════════════════════════════════════
mkdir -p "$BOT_DIR"

# Копируем бинарник
if [ -f "$BOT_PUBLISH_SRC" ]; then
    cp "$BOT_PUBLISH_SRC" "$BOT_BINARY"
    chmod +x "$BOT_BINARY"
    echo "[Wrapper] Бинарник скопирован → ${BOT_DIR}"
else
    echo "[Wrapper] ОШИБКА: Не найден ${BOT_PUBLISH_SRC}"
    exit 1
fi

# Копируем config.yml из исходников, если в bot/ его ещё нет
# (чтобы не перезаписать существующий конфиг при обновлении!)
BOT_CONFIG_SRC="${BOT_PROJECT}/config.yml"
if [ -f "$BOT_CONFIG_SRC" ] && [ ! -f "$BOT_CONFIG" ]; then
    cp "$BOT_CONFIG_SRC" "$BOT_CONFIG"
    echo "[Wrapper] Конфиг скопирован → ${BOT_CONFIG}"
fi

# Проверяем, что конфиг на месте
if [ ! -f "$BOT_CONFIG" ]; then
    echo "[Wrapper] ВНИМАНИЕ: ${BOT_CONFIG} не найден!"
    echo "[Wrapper] Создай файл со следующим содержимым:"
    echo "  bot_token: \"YOUR_DISCORD_BOT_TOKEN\""
    echo "  guild_id: 1234567890123456789"
    echo "  database_engine: sqlite"
    echo "  database_sqlite_path: bot.db"
fi

# ═══════════════════════════════════════════════════════════════
# Шаг 3: Цикл бота (авто-перезапуск, независим от сервера)
# ═══════════════════════════════════════════════════════════════
bot_loop() {
    while [ $SHUTDOWN -eq 0 ]; do
        echo "[Wrapper] Запуск Egide.Bot из ${BOT_DIR}..."
        
        # Рабочая директория бота — BOT_DIR, чтобы он нашёл config.yml
        (cd "$BOT_DIR" && ./Egide.Bot) &
        BOT_PID=$!
        
        wait $BOT_PID
        BOT_EXIT=$?
        
        if [ $SHUTDOWN -ne 0 ]; then
            echo "[Wrapper] Бот остановлен (код: $BOT_EXIT), shutdown mode."
            break
        fi
        
        echo "[Wrapper] Egide.Bot упал (код: $BOT_EXIT). Перезапуск через ${BOT_RESTART_DELAY}с..."
        sleep "$BOT_RESTART_DELAY"
    done
}

bot_loop &
BOT_LOOP_PID=$!

sleep 2
if [ -n "$BOT_PID" ] && kill -0 "$BOT_PID" 2>/dev/null; then
    echo "[Wrapper] Egide.Bot активен (PID: $BOT_PID)"
else
    echo "[Wrapper] ПРЕДУПРЕЖДЕНИЕ: Бот не запустился. Проверь ${BOT_CONFIG}."
fi

# ═══════════════════════════════════════════════════════════════
# Шаг 4: Запуск сервера (якорь)
# ═══════════════════════════════════════════════════════════════
echo "[Wrapper] Запуск сервера (якорь)..."

dotnet run --project Content.Packaging server --hybrid-acz --platform linux &
SERVER_PID=$!

wait $SERVER_PID
SERVER_EXIT=$?

echo "[Wrapper] Сервер завершился (код: $SERVER_EXIT). Останавливаем бота..."

# ═══════════════════════════════════════════════════════════════
# Шаг 5: Сервер умер — останавливаем бота
# ═══════════════════════════════════════════════════════════════
SHUTDOWN=1

if [ -n "$BOT_LOOP_PID" ] && kill -0 "$BOT_LOOP_PID" 2>/dev/null; then
    kill "$BOT_LOOP_PID" 2>/dev/null
    wait "$BOT_LOOP_PID" 2>/dev/null
fi

if [ -n "$BOT_PID" ] && kill -0 "$BOT_PID" 2>/dev/null; then
    kill -TERM "$BOT_PID" 2>/dev/null
    wait "$BOT_PID" 2>/dev/null
fi

echo "[Wrapper] Wrapper завершён."
exit $SERVER_EXIT
