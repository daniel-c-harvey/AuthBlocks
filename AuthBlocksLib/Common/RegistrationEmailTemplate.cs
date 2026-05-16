using System.Net;

namespace AuthBlocksLib.Common;

public static class RegistrationEmailTemplate
{
    public static string Create(string token, string link, string applicationName, string supportEmail)
    {
        var encodedAppName = WebUtility.HtmlEncode(applicationName);
        var encodedSupportEmail = WebUtility.HtmlEncode(supportEmail);
        var encodedSupportEmailHref = Uri.EscapeDataString(supportEmail);

        // When no support email is configured the host wants the line to still read naturally —
        // collapse the "reach us at <link>" sentence to "contact support" rather than leaving a dangling preposition.
        var supportContactHtml = string.IsNullOrWhiteSpace(supportEmail)
            ? "If you have any questions or need assistance during the registration process, our support team is here to help. Please contact support or visit our help center."
            : $"""
              If you have any questions or need assistance during the registration process, our support team is here to help. You can reach us at
              <a href="mailto:{encodedSupportEmailHref}" style="color: #00796B; text-decoration: none;">{encodedSupportEmail}</a>
              or visit our help center.
              """;

        return $$"""
                 <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
                 <html xmlns="http://www.w3.org/1999/xhtml">
                 <head>
                     <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
                     <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                     <title>Welcome to {{encodedAppName}}</title>
                 </head>
                 <body style="margin: 0; padding: 0; background-color: #fafafa; font-family: Arial, Helvetica, sans-serif;">
                     <!-- Main Container Table -->
                     <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #fafafa;">
                         <tr>
                             <td align="center" style="padding: 20px 0;">
                                 <!-- Email Content Table -->
                                 <table cellpadding="0" cellspacing="0" border="0" width="600" style="max-width: 600px; background-color: #ffffff; margin: 0 auto;">

                                     <!-- Header -->
                                     <tr>
                                         <td align="center" style="background-color: #00796B; padding: 40px 30px;">
                                             <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                                 <tr>
                                                     <td align="center">
                                                         <h1 style="color: #ffffff; font-size: 28px; font-weight: bold; margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">Welcome to {{encodedAppName}}</h1>
                                                         <p style="color: #ffffff; font-size: 16px; margin: 8px 0 0 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">Complete your account registration</p>
                                                     </td>
                                                 </tr>
                                             </table>
                                         </td>
                                     </tr>

                                     <!-- Main Content -->
                                     <tr>
                                         <td style="padding: 40px 30px; background-color: #ffffff;">
                                             <table cellpadding="0" cellspacing="0" border="0" width="100%">

                                                 <!-- Welcome Text -->
                                                 <tr>
                                                     <td style="padding-bottom: 30px;">
                                                         <p style="color: #37474F; font-size: 18px; line-height: 1.6; margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">
                                                             Hello and welcome! We're excited to have you join the {{encodedAppName}} family. To complete your account setup and start managing your marina operations, please use the registration information below.
                                                         </p>
                                                     </td>
                                                 </tr>

                                                 <!-- Registration Code Box -->
                                                 <tr>
                                                     <td align="center" style="padding: 30px 0;">
                                                         <table cellpadding="0" cellspacing="0" border="2" width="100%" style="background-color: #f5f5f5; border-color: #e0e0e0; border-style: solid;">
                                                             <tr>
                                                                 <td align="center" style="padding: 25px;">
                                                                     <p style="color: #546E7A; font-size: 14px; font-weight: bold; text-transform: uppercase; margin: 0 0 10px 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">Your Registration Code</p>
                                                                     <table cellpadding="0" cellspacing="0" border="1" style="margin: 10px 0; border-color: #009688; border-style: solid; background-color: #ffffff;">
                                                                         <tr>
                                                                             <td align="center" style="padding: 15px 20px;">
                                                                                 <span style="color: #00796B; font-size: 24px; font-weight: bold; font-family: 'Courier New', Courier, monospace;">{{token}}</span>
                                                                             </td>
                                                                         </tr>
                                                                     </table>
                                                                     <p style="color: #546E7A; font-size: 12px; margin: 10px 0 0 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">
                                                                         Please keep this code secure and use it during registration
                                                                     </p>
                                                                 </td>
                                                             </tr>
                                                         </table>
                                                     </td>
                                                 </tr>

                                                 <!-- Call to Action Button -->
                                                 <tr>
                                                     <td align="center" style="padding: 25px 0;">
                                                         <table cellpadding="0" cellspacing="0" border="0">
                                                             <tr>
                                                                 <td align="center" style="background-color: #00796B; padding: 16px 32px;">
                                                                     <a href="{{link}}" style="color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; font-family: Arial, Helvetica, sans-serif; display: block;">Complete Registration →</a>
                                                                 </td>
                                                             </tr>
                                                         </table>
                                                     </td>
                                                 </tr>

                                                 <!-- Divider -->
                                                 <tr>
                                                     <td style="padding: 25px 0;">
                                                         <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                                             <tr>
                                                                 <td style="border-top: 1px solid #e0e0e0; font-size: 1px; line-height: 1px;">&nbsp;</td>
                                                             </tr>
                                                         </table>
                                                     </td>
                                                 </tr>

                                                 <!-- Instructions -->
                                                 <tr>
                                                     <td style="background-color: #f9f9f9; border-left: 4px solid #009688; padding: 20px;">
                                                         <h3 style="color: #00796B; font-size: 16px; margin: 0 0 12px 0; padding: 0; font-weight: bold; font-family: Arial, Helvetica, sans-serif;">Registration Steps:</h3>
                                                         <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                                             <tr>
                                                                 <td style="color: #37474F; font-size: 14px; line-height: 1.6; font-family: Arial, Helvetica, sans-serif;">
                                                                     <p style="margin: 0 0 8px 0; padding: 0;">1. Click the "Complete Registration" button above</p>
                                                                     <p style="margin: 0 0 8px 0; padding: 0;">2. Enter your registration code when prompted</p>
                                                                     <p style="margin: 0 0 8px 0; padding: 0;">3. Fill in your account details</p>
                                                                     <p style="margin: 0 0 8px 0; padding: 0;">4. Set up your secure password</p>
                                                                     <p style="margin: 0 0 0 0; padding: 0;">5. Start exploring {{encodedAppName}}!</p>
                                                                 </td>
                                                             </tr>
                                                         </table>
                                                     </td>
                                                 </tr>

                                                 <!-- Support Text -->
                                                 <tr>
                                                     <td style="padding: 25px 0 0 0;">
                                                         <p style="color: #546E7A; font-size: 14px; line-height: 1.6; margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">
                                                             {{supportContactHtml}}
                                                         </p>
                                                     </td>
                                                 </tr>

                                             </table>
                                         </td>
                                     </tr>

                                     <!-- Footer -->
                                     <tr>
                                         <td style="background-color: #f5f5f5; padding: 30px; border-top: 1px solid #e0e0e0;">
                                             <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                                 <tr>
                                                     <td align="center">
                                                         <p style="color: #546E7A; font-size: 14px; line-height: 1.5; margin: 0 0 15px 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">
                                                             This registration link will expire in 7 days for security purposes.
                                                         </p>
                                                         <p style="margin: 0 0 15px 0; padding: 0;">
                                                             <a href="#" style="color: #00796B; text-decoration: none; font-size: 13px; margin: 0 10px; font-family: Arial, Helvetica, sans-serif;">Privacy Policy</a>
                                                             <a href="#" style="color: #00796B; text-decoration: none; font-size: 13px; margin: 0 10px; font-family: Arial, Helvetica, sans-serif;">Terms of Service</a>
                                                             <a href="#" style="color: #00796B; text-decoration: none; font-size: 13px; margin: 0 10px; font-family: Arial, Helvetica, sans-serif;">Support</a>
                                                         </p>
                                                         <p style="color: #9e9e9e; font-size: 12px; margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif;">
                                                             © 2026 {{encodedAppName}}. All rights reserved.
                                                         </p>
                                                     </td>
                                                 </tr>
                                             </table>
                                         </td>
                                     </tr>

                                 </table>

                                 <!-- Mobile Fallback Table -->
                                 <!--[if (gte mso 9)|(IE)]>
                                 </td>
                                 </tr>
                                 </table>
                                 <![endif]-->

                             </td>
                         </tr>
                     </table>
                 </body>
                 </html>
                 """;
    }
}
