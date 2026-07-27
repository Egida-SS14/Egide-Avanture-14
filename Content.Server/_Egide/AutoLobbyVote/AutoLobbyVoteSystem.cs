using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;

namespace Content.Server._Egide.AutoLobbyVote;
{
    /// <summary>
    /// Система для автоматического запуска голосований за карту и режим при переходе в лобби.
    /// </summary>
    public sealed class AutoLobbyVoteSystem : EntitySystem
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        }

        private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
        {
            if (ev.New == GameRunLevel.PreRoundLobby) // Если запустилось лобби
            {
                StartLobbyVotes();
            }
        }

        private void StartLobbyVotes()
        {
            // Голосование за режим
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset); // null - заспустил сервер
            // Голосование за карту
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);
        }
    }
}
