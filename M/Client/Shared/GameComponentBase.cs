using M.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace M.Client.Shared
{
    public class GameComponentBase : ComponentBase
    {
        [CascadingParameter] public GameClient Client { get; set; }
    }
}
