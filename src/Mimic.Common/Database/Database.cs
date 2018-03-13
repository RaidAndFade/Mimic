using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.Common
{
    public interface IDatabase
    {
        void createIfNonexistant();
        Task init();
        Task<AccountInfo> AsyncFetchAccountByName(string name);
        Task<AccountInfo> AsyncFetchAccountById(int id);
        Task AsyncUpdateAccount(AccountInfo info);
    }
}
