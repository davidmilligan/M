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
    }
}
