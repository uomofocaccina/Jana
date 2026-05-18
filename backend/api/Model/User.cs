namespace JanaApi.Model;

public class UserMinimal
{
    public int id { get; set; }
    public string username { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public short active { get; set; } = 1;
    public short admin { get; set; } = 0;
    public DateTime created { get; set; }
}

public class  User: UserMinimal
{
    public string password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}

public class LoginResult : Result
{
    public string token { get; set; } = string.Empty;
    public bool admin { get; set; } = false;
}

public class ChangePasswordRequest
{
    public string oldPassword { get; set; } = string.Empty;
    public string newPassword { get; set; } = string.Empty;
}

public class ChangePasswordAdminRequest
{
    public int id { get; set; }
    public string password { get; set; } = string.Empty;
}