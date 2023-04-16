using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.Hex.HexTypes;

namespace SquidNFTBot
{
    class Program
    {
        public static List<Bot> bots { get; set; } = new List<Bot>();
        static void Main(string[] args)
        {
            Account account = new Account("privateKey", 56);
            Wallet wallet = new Wallet("metamask1", account, new CancellationTokenSource());
            bots.Add(new Bot(wallet));

            StartAccounts();

            Console.WriteLine("/add {accountName} {privateKey} - to add new account");
            Console.WriteLine("/run {accountName} - to run account");
            Console.WriteLine("/stop {accountName} - to stop account");

            while (true)
            {
                string command = Console.ReadLine();
                if (command.Contains("/add"))
                {
                    string[] data = command.Split(' ');

                    AddNewAccount(data[1], data[2]);
                }
                else if (command.Contains("/stop"))
                {
                    string[] data = command.Split(' ');

                    StopAccount(data[1]);
                }
                else if (command.Contains("/run"))
                {
                    string[] data = command.Split(' ');

                    StartAccount(data[1]);
                }
            }
        }
        private static void StartAccount(string accountName)
        {
            Bot bot = FindAccount(accountName);
            bot?.Monitoring();
        }
        private static void StopAccount(string accountName)
        {
            Bot bot = FindAccount(accountName);
            bot?.wallet?.cancellationTokenSource?.Cancel();

            Logger.logAdd($"[{accountName}] Account has been stopped", ConsoleColor.Yellow);
        }
        private static Bot FindAccount(string accountName)
        {
            Bot bot = bots.Find(x => x.wallet.accountName == accountName);
            return bot;
        }
        private static void AddNewAccount(string accountName, string privateKey)
        {
            Account account = new Account(privateKey, 56);
            Wallet wallet = new Wallet(accountName, account, new CancellationTokenSource());
            bots.Add(new Bot(wallet));
            Logger.logAdd($"New account {accountName} successfully added", ConsoleColor.Green);
        }
        private static void StartAccounts()
        {
            for (int i = 0; i < bots.Count; i++)
            {
                bots[i].Monitoring();
            }
        }
    }
}
