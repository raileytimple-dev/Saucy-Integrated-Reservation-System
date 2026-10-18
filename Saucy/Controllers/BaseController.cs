using Microsoft.AspNetCore.Mvc;
namespace SaucyMVC.Controllers;
public abstract class BaseController:Controller {
 protected int? CurrentUserId=>HttpContext.Session.GetInt32("UserId"); protected string? CurrentRole=>HttpContext.Session.GetString("Role");
 protected string? CurrentUserName=>HttpContext.Session.GetString("FullName");
 protected IActionResult LoginRequired(){TempData["Error"]="Please sign in first.";return RedirectToAction("Login","Account");}
 protected bool IsRole(params string[] roles)=>CurrentUserId.HasValue && roles.Contains(CurrentRole,StringComparer.OrdinalIgnoreCase);
}
