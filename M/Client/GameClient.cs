using M.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace M.Client
{
    public class GameClient : IDisposable
    {
        private HubConnection hub;

        public Guid Id { get; set; }
        public Game CurrentGame { get; set; }
        public string CurrentUser { get; set; }
        public bool IsLoading { get; set; }
        public Player Me { get; set; }
        public Player TurnPlayer { get; set; }
        public Location CurrentLocation { get; set; }
        public AccessToken AccessToken { get; set; }
        public bool IsConnected => hub?.State == HubConnectionState.Connected;
        public bool IsConnecting => hub?.State == HubConnectionState.Connecting || hub?.State == HubConnectionState.Reconnecting;
        public bool IsDisconnected => hub?.State == HubConnectionState.Disconnected;
        public bool IsCurrentOwner => CurrentGame != null && CurrentGame.Owner == CurrentUser;
        public bool IsStartGameEnabled => IsCurrentOwner && !CurrentGame.IsStarted;
        public bool IsEndGameEnabled => IsCurrentOwner;
        public bool IsRetireEnabled => CurrentGame != null;
        public Action StateHasChanged { get; set; }
        public Action<string> OnError { get; set; }
        public Action GameOver { get; set; }
        public Func<string, Task<bool>> ConfirmAsync { get; set; }
        public Action<int> Sell { get; set; }
        public NavigationManager NavigationManager { get; }
        public IAccessTokenProvider AccessTokenProvider { get; }

        public GameClient(NavigationManager navigationManager, IAccessTokenProvider accessTokenProvider)
        {
            NavigationManager = navigationManager;
            AccessTokenProvider = accessTokenProvider;
        }

        public async Task Initialize(Guid id, string currentUser, Action stateHasChanged)
        {
            Id = id;
            CurrentUser = currentUser;
            StateHasChanged = stateHasChanged;
            await Join(id);
        }

        public Task Join(Guid game) => Hub(game);
        public Task Refresh() => Join(Id);
        public Task Admit(string user) => Hub(CurrentGame.Id, (object)user);
        public Task Start() => Hub(CurrentGame.Id);
        public Task Message(string message) => Hub(CurrentGame.Id, (object)message);
        public Task Roll() => Hub(CurrentGame.Id);
        public Task Pay() => Hub(CurrentGame.Id);
        public Task PayPlayerDebt() => Hub(CurrentGame.Id);
        public Task GetOutOfJailFree() => Hub(CurrentGame.Id);
        public Task Buy() => Hub(CurrentGame.Id);
        public Task BuyProperty(int position) => Hub(CurrentGame.Id, position);
        public Task DoNotBuyProperty(int position) => Hub(CurrentGame.Id, position);
        public Task Bid(decimal amount) => Hub(CurrentGame.Id, amount);
        public Task DoNotBuy() => Hub(CurrentGame.Id);
        public Task ForSale(int position, decimal amount, string to) => Hub(CurrentGame.Id, position, amount, (object)to);
        public Task Upgrade(int position) => Confirm(() => Hub(CurrentGame.Id, position));
        public Task Mortgage(int position) => Confirm(() => Hub(CurrentGame.Id, position));
        public Task PayMortgage(int position) => Confirm(() => Hub(CurrentGame.Id, position));
        public Task EndTurn() => Hub(CurrentGame.Id);
        public Task Retire() => Confirm(() => Hub(CurrentGame.Id));
        public Task End() => Confirm(() => Hub(CurrentGame.Id));
        public Task SetIcon(string icon) => Hub(CurrentGame.Id, (object)icon);

        private Task Hub(object arg0, [CallerMemberName] string name = "") => HubInvoke(name, arg0);
        private Task Hub(object arg0, object arg1, [CallerMemberName] string name = "") => HubInvoke(name, arg0, arg1);
        private Task Hub(object arg0, object arg1, object arg2, [CallerMemberName] string name = "") => HubInvoke(name, arg0, arg1, arg2);
        private Task Hub(object arg0, object arg1, object arg2, object arg3, [CallerMemberName] string name = "") => HubInvoke(name, arg0, arg1, arg2, arg3);

        private async Task HubInvoke(string methodName, params object[] args)
        {
            try
            {
                IsLoading = true;
                await StartHub();
                CurrentGame = await (args.Length switch
                {
                    0 => hub.InvokeAsync<Game>(methodName),
                    1 => hub.InvokeAsync<Game>(methodName, args[0]),
                    2 => hub.InvokeAsync<Game>(methodName, args[0], args[1]),
                    3 => hub.InvokeAsync<Game>(methodName, args[0], args[1], args[2]),
                    4 => hub.InvokeAsync<Game>(methodName, args[0], args[1], args[2], args[3]),
                    _ => throw new NotSupportedException()
                });
                UpdateGameData();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task StartHub()
        {
            if (!IsConnected)
            {
                if (hub != null)
                {
                    await hub.DisposeAsync();
                }
                hub = new HubConnectionBuilder()
                    .WithUrl(NavigationManager.ToAbsoluteUri("/mhub"), options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            if (AccessToken != null && AccessToken.Expires > DateTimeOffset.Now)
                            {
                                return AccessToken.Value;
                            }
                            if ((await AccessTokenProvider.RequestAccessToken()).TryGetToken(out var token))
                            {
                                AccessToken = token;
                                return token.Value;
                            }
                            return null;
                        };
                    })
                    .Build();
                hub.On<Game>("Update", game =>
                {
                    if (!game.IsActive)
                    {
                        GameOver?.Invoke();
                    }
                    CurrentGame = game;
                    UpdateGameData();
                    StateHasChanged();
                });
                hub.On<string>("Error", errorMessage =>
                {
                    OnError(errorMessage);
                });
                hub.Closed += Closed;
                hub.Reconnected += Reconnected;
                hub.Reconnecting += Reconnecting;
                await hub.StartAsync();
                StateHasChanged();
            }
        }

        private async Task Confirm(Func<Task> action)
        {
            if (await ConfirmAsync("Are you sure?"))
            {
                await action();
            }
        }

        private void UpdateGameData()
        {
            Me = CurrentGame?.Players.FirstOrDefault(t => t.Name == CurrentUser);
            TurnPlayer = CurrentGame?.Turn != null ? CurrentGame.Players.FirstOrDefault(t => t.Name == CurrentGame.Turn) : null;
            CurrentLocation = CurrentGame.LastRoll1 != 0 ? CurrentGame.Locations.FirstOrDefault(t => t.Position == TurnPlayer?.Position) : null;
        }

        private Task Reconnected(string args)
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task Reconnecting(Exception ex)
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task Closed(Exception ex)
        {
            if (ex != null)
            {
                OnError(new StringBuilder().AppendLine("Connection Error").Append(ex.Message).ToString());
            }
            IsLoading = false;
            StateHasChanged();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _ = hub.DisposeAsync();
        }
    }
}
