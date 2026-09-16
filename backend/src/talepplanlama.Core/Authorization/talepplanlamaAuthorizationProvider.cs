using Abp.Authorization;
using Abp.Localization;

namespace talepplanlama.Authorization
{
    public class talepplanlamaAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            var pages = context.GetPermissionOrNull("Pages") ?? context.CreatePermission("Pages", L("Pages"));

            // RequestType
            pages.CreateChildPermission(PermissionNames.RequestType_Read, L("RequestType.Read"));
            pages.CreateChildPermission(PermissionNames.RequestType_Create, L("RequestType.Create"));
            pages.CreateChildPermission(PermissionNames.RequestType_Update, L("RequestType.Update"));
            pages.CreateChildPermission(PermissionNames.RequestType_Delete, L("RequestType.Delete"));

            // ChangeRequest
            pages.CreateChildPermission(PermissionNames.ChangeRequest_Read, L("ChangeRequest.Read"));
            pages.CreateChildPermission(PermissionNames.ChangeRequest_Create, L("ChangeRequest.Create"));
            pages.CreateChildPermission(PermissionNames.ChangeRequest_Update, L("ChangeRequest.Update"));
            pages.CreateChildPermission(PermissionNames.ChangeRequest_Delete, L("ChangeRequest.Delete"));
            pages.CreateChildPermission(PermissionNames.ChangeRequest_ChangeStatus, L("ChangeRequest.ChangeStatus"));

            // ImplementationLog
            pages.CreateChildPermission(PermissionNames.ImplementationLog_Read, L("ImplementationLog.Read"));
            pages.CreateChildPermission(PermissionNames.ImplementationLog_Create, L("ImplementationLog.Create"));
            pages.CreateChildPermission(PermissionNames.ImplementationLog_Update, L("ImplementationLog.Update"));
            pages.CreateChildPermission(PermissionNames.ImplementationLog_Delete, L("ImplementationLog.Delete"));

            // RBAC
            pages.CreateChildPermission(PermissionNames.AppUser_Read, L("AppUser.Read"));
            pages.CreateChildPermission(PermissionNames.AppRole_Read, L("AppRole.Read"));
            pages.CreateChildPermission(PermissionNames.AppUser_Create, L("AppUser.Create"));
            pages.CreateChildPermission(PermissionNames.AppRole_Create, L("AppRole.Create"));
            pages.CreateChildPermission(PermissionNames.AppUser_Update, L("AppUser.Update"));
            pages.CreateChildPermission(PermissionNames.AppRole_Update, L("AppRole.Update"));
            pages.CreateChildPermission(PermissionNames.AppUser_Delete, L("AppUser.Delete"));
            pages.CreateChildPermission(PermissionNames.AppRole_Delete, L("AppRole.Delete"));
            pages.CreateChildPermission(PermissionNames.AppRole_AssignPermissions, L("AppRole.AssignPermissions"));
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, talepplanlamaConsts.LocalizationSourceName);
        }
    }
}
