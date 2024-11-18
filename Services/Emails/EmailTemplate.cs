namespace UserAuthentication_ASPNET.Services.Emails;

public static class EmailTemplate
{
    public static string GetEmailTemplateForResetPassword(string subject, string content)
    {
        return """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Password Reset Request</title>
            </head>
            <body style="margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #F4F4F4;">
                <table style="background-color: #FFFFFF; margin-top: 20px; padding: 20px; width: 100%; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);">
                    <tr>
                        <td align="center" style="padding: 20px;">
                            <h1 style="color: #333333; font-size: 24px;">{{subject}}</h1>
                        </td>
                    </tr>
                    <tr>
                        <td align="center" style="padding: 20px;">
                            <p style="color: #555555; font-size: 16px;">We received a request to reset your password. If you did not request a password reset, please ignore this email.</p>
                            <p style="color: #555555; font-size: 16px;">To reset your password, click the link below:</p>
                            <a href="{{resetLink}}" style="background-color: #007BFF; color: #FFFFFF; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px;">Reset Password</a>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """.Replace("{{subject}}", subject).Replace("{{resetLink}}", content);
    }
}
