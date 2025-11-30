using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using Microsoft.Extensions.Options;
using Core.Configurations;

namespace Pages
{
    public class AuthModel(IOptions<App> appInfo) : PageModel
    {
    }
}