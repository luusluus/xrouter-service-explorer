using BitcoinLib.Services.Coins.Blocknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Xrouter.Service.Explorer.Extensions
{
    public static class IQueryableExtensions
    {
        public static IQueryable<ServiceNodeResponse> ApplyServiceNodeFiltering(this IQueryable<ServiceNodeResponse> query, ServiceNodeQuery queryObj)
        {
            if (queryObj.OnlyXWallets)
            {
                //query = query.Where(sn => !sn.XWallets.Contains(""));
            }
            if (!string.IsNullOrWhiteSpace(queryObj.SpvWallet))
                query = query.Where(sn => sn.SpvWallets.Contains(queryObj.SpvWallet));

            if (!string.IsNullOrWhiteSpace(queryObj.XCloudService))
                query = query.Where(sn => sn.XCloudServices.Contains(queryObj.XCloudService));
            return query;
        }

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, IQueryObject queryObj)
        {
            if (queryObj.Page <= 0)
                queryObj.Page = 1;

            if (queryObj.PageSize <= 0)
                queryObj.PageSize = 10;

            return query.Skip((queryObj.Page - 1) * queryObj.PageSize).Take(queryObj.PageSize);
        }
    }
}