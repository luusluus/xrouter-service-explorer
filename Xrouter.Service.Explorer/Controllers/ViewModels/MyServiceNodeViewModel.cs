﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xrouter.Service.Explorer.Controllers.ViewModels
{
    public class MyServiceNodeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public bool Ownership { get; set; }
        public string Status { get; set; }
        public string ApplicationUserId { get; set; }
    }
}
