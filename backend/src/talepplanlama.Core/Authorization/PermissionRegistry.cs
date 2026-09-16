using System.Collections.Generic;
using Abp.Dependency;

namespace talepplanlama.Authorization
{
    /// <summary>Single permission descriptor — name, group (entity), description.</summary>
    public class PermissionInfo
    {
        public string Name { get; }
        public string Group { get; }
        public string Description { get; }
        public bool IsRbac { get; }

        public PermissionInfo(string name, string group, string description, bool isRbac)
        {
            Name = name; Group = group; Description = description; IsRbac = isRbac;
        }
    }

    public interface IPermissionRegistry
    {
        IReadOnlyList<PermissionInfo> All { get; }
    }

    public class PermissionRegistry : IPermissionRegistry, ISingletonDependency
    {
        public IReadOnlyList<PermissionInfo> All { get; } = new List<PermissionInfo>
        {
            new PermissionInfo("RequestType.Read", "RequestType", "Read RequestType", false),
            new PermissionInfo("RequestType.Create", "RequestType", "Create RequestType", false),
            new PermissionInfo("RequestType.Update", "RequestType", "Update RequestType", false),
            new PermissionInfo("RequestType.Delete", "RequestType", "Delete RequestType", false),
            new PermissionInfo("ChangeRequest.Read", "ChangeRequest", "Read ChangeRequest", false),
            new PermissionInfo("ChangeRequest.Create", "ChangeRequest", "Create ChangeRequest", false),
            new PermissionInfo("ChangeRequest.Update", "ChangeRequest", "Update ChangeRequest", false),
            new PermissionInfo("ChangeRequest.Delete", "ChangeRequest", "Delete ChangeRequest", false),
            new PermissionInfo("ChangeRequest.ChangeStatus", "ChangeRequest", "Change ChangeRequest status", false),
            new PermissionInfo("ImplementationLog.Read", "ImplementationLog", "Read ImplementationLog", false),
            new PermissionInfo("ImplementationLog.Create", "ImplementationLog", "Create ImplementationLog", false),
            new PermissionInfo("ImplementationLog.Update", "ImplementationLog", "Update ImplementationLog", false),
            new PermissionInfo("ImplementationLog.Delete", "ImplementationLog", "Delete ImplementationLog", false),
            new PermissionInfo("AppUser.Read", "AppUser", "Read users", true),
            new PermissionInfo("AppRole.Read", "AppRole", "Read roles", true),
            new PermissionInfo("AppUser.Create", "AppUser", "Create users", true),
            new PermissionInfo("AppRole.Create", "AppRole", "Create roles", true),
            new PermissionInfo("AppUser.Update", "AppUser", "Update users", true),
            new PermissionInfo("AppRole.Update", "AppRole", "Update roles", true),
            new PermissionInfo("AppUser.Delete", "AppUser", "Delete users", true),
            new PermissionInfo("AppRole.Delete", "AppRole", "Delete roles", true),
            new PermissionInfo("AppRole.AssignPermissions", "AppRole", "Assign permissions to roles", true),
        };
    }
}
