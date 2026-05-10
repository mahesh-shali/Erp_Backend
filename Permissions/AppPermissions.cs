namespace Erp.Api.Permissions;

public static class AppPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string AiChatView = "ai-chat.view";
    public const string MasterView = "master.view";
    public const string SalesView = "sales.view";
    public const string OutsourcingView = "outsourcing.view";
    public const string ProductionView = "production.view";
    public const string InventoryView = "inventory.view";
    public const string PlanningView = "planning.view";
    public const string CashFlowView = "cash-flow.view";
    public const string InspectionView = "inspection.view";
    public const string MaintenanceView = "maintenance.view";
    public const string HumanResourceView = "human-resource.view";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string DepartmentsView = "departments.view";
    public const string DepartmentsManage = "departments.manage";

    public static readonly string[] All =
    [
        DashboardView,
        AiChatView,
        MasterView,
        SalesView,
        OutsourcingView,
        ProductionView,
        InventoryView,
        PlanningView,
        CashFlowView,
        InspectionView,
        MaintenanceView,
        HumanResourceView,
        UsersView,
        UsersManage,
        RolesView,
        RolesManage,
        DepartmentsView,
        DepartmentsManage
    ];
}
