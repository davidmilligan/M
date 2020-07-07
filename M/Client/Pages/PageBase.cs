using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace M.Client.Pages
{
    public class PageBase : ComponentBase
    {
        [Inject] public IJSRuntime JsRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; }
        public string CurrentUser { get; set; }
        public bool IsLoading { get; set; }

        protected override async Task OnInitializedAsync()
        {
            CurrentUser = (await AuthenticationStateTask).User.Identity.Name;
        }
        protected async Task Alert(string message) => await JsRuntime.InvokeVoidAsync("alert", message);
        protected async Task<string> Prompt(string message) => await JsRuntime.InvokeAsync<string>("prompt", message);
        protected async Task<bool> Confirm(string message) => await JsRuntime.InvokeAsync<bool>("confirm", message);

        public static string FormatDateRelativeToNow(DateTimeOffset date) => date switch
        {
            var d when (DateTimeOffset.Now - d).TotalMinutes <= 1.0 => "Just Now",
            var d when (DateTimeOffset.Now - d).TotalHours < 1.0 => $"{Math.Round((DateTimeOffset.Now - d).TotalMinutes)} minutes ago",
            var d when d.Date == DateTimeOffset.Now.Date => $"{Math.Round((DateTimeOffset.Now - d).TotalHours)} hours ago",
            var d when d > DateTimeOffset.Now.Date.AddDays(-1) => $"yesterday at {d.LocalDateTime.ToShortTimeString()}",
            var d when d > DateTimeOffset.Now.Date.AddDays(-7) => $"{d.DayOfWeek} at {d.LocalDateTime.ToShortTimeString()}",
            _ => date.ToString(),
        };
    }
}
