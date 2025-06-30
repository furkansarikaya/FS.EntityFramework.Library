using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Extensions;

using System.Runtime.CompilerServices;

public static class DbContextSoftDeleteExtensions
{
    private static readonly ConditionalWeakTable<DbContext, Dictionary<string, object>> Store = new();

    private const string BypassKey = "__BypassSoftDeleteInterceptor__";

    public static void EnableBypassSoftDelete(this DbContext context)
    {
        GetOrCreate(context)[BypassKey] = true;
    }

    public static void DisableBypassSoftDelete(this DbContext context)
    {
        GetOrCreate(context)[BypassKey] = false;
    }

    public static bool IsBypassSoftDeleteEnabled(this DbContext context)
    {
        return GetOrCreate(context).TryGetValue(BypassKey, out var value) &&
               value is bool and true;
    }

    private static Dictionary<string, object> GetOrCreate(DbContext context)
    {
        return Store.GetOrCreateValue(context);
    }
}