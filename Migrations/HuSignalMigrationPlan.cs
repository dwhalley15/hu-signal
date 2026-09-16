using Umbraco.Cms.Core.Packaging;

public class HuSignalMigrationPlan : PackageMigrationPlan
{
    public HuSignalMigrationPlan()
        : base("Hu Signal")
    {
    }

    public override bool IgnoreCurrentState => false;

    protected override void DefinePlan()
    {
        To<CreateClarityTables>(
            new Guid("AB9AF7FA-9E3A-460C-B1F4-F29057ED2031"));
    }
}