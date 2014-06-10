// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;

namespace Microsoft.AspNet.Identity.Test
{
    public static class MockHelpers
    {
        public static UserManager<TUser> CreateManager<TUser>(Func<IUserStore<TUser>> storeFunc) where TUser : class
        {
            var services = new ServiceCollection();
            services.Add(OptionsServices.GetDefaultServices());
            services.AddIdentity<TUser>(b => b.AddUserStore(storeFunc));
            services.SetupOptions<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.AllowOnlyAlphanumericNames = false;
            });
            return services.BuildServiceProvider().GetService<UserManager<TUser>>();
        }

        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var options = new OptionsAccessor<IdentityOptions>(null);
            return new Mock<UserManager<TUser>>(new ServiceCollection().BuildServiceProvider(), store.Object, options);
        }

        public static UserManager<TUser> TestUserManager<TUser>() where TUser : class
        {
            return TestUserManager(new Mock<IUserStore<TUser>>().Object);
        }

        public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store) where TUser : class
        {
            var options = new OptionsAccessor<IdentityOptions>(null);
            var validator = new Mock<UserValidator<TUser>>(new IdentityResourceManager());
            var userManager = new UserManager<TUser>(new ServiceCollection().BuildServiceProvider(), store, options);
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>(), CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;
            return userManager;
        }
    }
}