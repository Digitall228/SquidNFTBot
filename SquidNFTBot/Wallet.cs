using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SquidNFTBot
{
    public class Wallet
    {
        public string accountName { get; set; }
        public Account account { get; set; }
        public CancellationTokenSource cancellationTokenSource{ get; private set; }
        public CancellationToken cancellationToken { get; set; }
        public Wallet(string AccountName, Account Account, CancellationTokenSource CancellationTokenSource)
        {
            accountName = AccountName;
            account = Account;
            cancellationTokenSource = CancellationTokenSource;

            cancellationToken = cancellationTokenSource.Token;
        }
    }
}
