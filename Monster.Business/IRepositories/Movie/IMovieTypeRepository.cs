/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹IMovieTypeRepository编写接口
 */
using Monster.Core.BaseProvider;
using Monster.Entity.DomainModels;
using Monster.Core.Extensions.AutofacManager;

namespace Monster.Business.IRepositories
{
    public partial interface IMovieTypeRepository : IDependency, IRepository<MovieType>
    {
    }
}
