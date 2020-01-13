﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xrouter.Service.Explorer.Core.Models;

namespace Xrouter.Service.Explorer.Persistence
{
    public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
    {
        public virtual DbSet<MyServicenode> ServiceNodes { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
