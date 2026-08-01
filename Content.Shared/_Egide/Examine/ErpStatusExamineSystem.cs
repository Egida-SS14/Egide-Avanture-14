using Content.Shared._Egide.Preferences;
using Content.Shared.DetailExaminable;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Shared._Egide.Examine;

public sealed class ErpStatusExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DetailExaminableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, DetailExaminableComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var name = Identity.Name(uid, EntityManager);
        if (name != MetaData(uid).EntityName)
            return;

        var text = component.ErpStatus switch
        {
            ErpStatus.Full => Loc.GetString("erp-status-examine-full"),
            ErpStatus.Incomplete => Loc.GetString("erp-status-examine-incomplete"),
            _ => Loc.GetString("erp-status-examine-none"),
        };

        args.PushMarkup(text, priority: -10);
    }
}
