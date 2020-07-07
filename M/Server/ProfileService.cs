using System;
using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using System.Threading.Tasks;

namespace M.Server
{
    public class ProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.IssuedClaims.AddRange(context.Subject.FindAll(JwtClaimTypes.Name));
            context.IssuedClaims.AddRange(context.Subject.FindAll(JwtClaimTypes.Role));
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context) => Task.CompletedTask;
    }
}
