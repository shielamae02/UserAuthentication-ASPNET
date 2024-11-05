namespace UserAuthentication_ASPNET.Models.Utils;

public static class Success
{
    public const string IS_AUTHENTICATED = "User has been authenticated successfully.";

    public static string RESOURCE_RETRIEVED(string resource)
    {
        return $"{resource} has been successfully retrieved.";
    }
    public static string RESOURCE_CREATED(string resource)
    {
        return $"{resource} has been successfully created.";
    }

    public static string RESOURCE_UPDATED(string resource)
    {
        return $"{resource} has been successfully updated.";
    }

    public static string RESOURCE_DELETED(string resource)
    {
        return $"{resource} has been successfully deleted.";
    }
}
