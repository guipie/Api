﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monster.Core.BaseProvider;
using Monster.Entity.DomainModels;
using Monster.Core.Extensions.AutofacManager;
namespace DairyStar.Builder.IRepositories
{
    public partial interface ISys_TableInfoRepository : IDependency,IRepository<Sys_TableInfo>
    {
    }
}

