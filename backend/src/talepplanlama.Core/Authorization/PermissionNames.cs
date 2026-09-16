namespace talepplanlama.Authorization
{
    public static class PermissionNames
    {
        public const string Pages = "Pages";

        // RequestType
        public const string RequestType_Read = "RequestType.Read";
        public const string RequestType_Create = "RequestType.Create";
        public const string RequestType_Update = "RequestType.Update";
        public const string RequestType_Delete = "RequestType.Delete";

        // ChangeRequest
        public const string ChangeRequest_Read = "ChangeRequest.Read";
        public const string ChangeRequest_Create = "ChangeRequest.Create";
        public const string ChangeRequest_Update = "ChangeRequest.Update";
        public const string ChangeRequest_Delete = "ChangeRequest.Delete";
        public const string ChangeRequest_ChangeStatus = "ChangeRequest.ChangeStatus";

        // ImplementationLog
        public const string ImplementationLog_Read = "ImplementationLog.Read";
        public const string ImplementationLog_Create = "ImplementationLog.Create";
        public const string ImplementationLog_Update = "ImplementationLog.Update";
        public const string ImplementationLog_Delete = "ImplementationLog.Delete";

        // RBAC management
        public const string AppUser_Read = "AppUser.Read";
        public const string AppRole_Read = "AppRole.Read";
        public const string AppUser_Create = "AppUser.Create";
        public const string AppRole_Create = "AppRole.Create";
        public const string AppUser_Update = "AppUser.Update";
        public const string AppRole_Update = "AppRole.Update";
        public const string AppUser_Delete = "AppUser.Delete";
        public const string AppRole_Delete = "AppRole.Delete";
        public const string AppRole_AssignPermissions = "AppRole.AssignPermissions";

    }
}
