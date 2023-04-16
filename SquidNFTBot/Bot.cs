using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.Hex.HexTypes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;
using Nethereum.Contracts.ContractHandlers;
using SquidNFTBot.SquidPlayerNFTContactABI;
using SquidNFTBot.MainSquidGameContractABI;

namespace SquidNFTBot
{
    public class Bot
    {
        public Wallet wallet{ get; set; }
        public Web3 web3 { get; set; }
        public string gameAddress { get; set; } = "0xccc78df56470b70cb901fcc324a8fbbe8ab5304b";
        public string playerNFTAddress { get; set; } = "0xb00ED7E3671Af2675c551a1C26Ffdcc5b425359b";
        public ContractHandler gameContract { get; set; }
        public ContractHandler playersContract { get; set; }
        public string abi { get; set; }

        public Bot(Wallet Wallet)
        {
            wallet = Wallet;

            web3 = new Web3(wallet.account, "https://bsc-dataseed.binance.org/");
            web3.TransactionManager.UseLegacyAsDefault = true;

            gameContract = web3.Eth.GetContractHandler(gameAddress);
            playersContract = web3.Eth.GetContractHandler(playerNFTAddress);
        }
        public async void Monitoring()
        {
            Logger.logAdd("Start monitoring", ConsoleColor.Green);

            List<TokensViewFront> players = await ParsePlayers();

            while (!wallet.cancellationToken.IsCancellationRequested)
            {
                List<TokensViewFront> availablePlayers = GetAvailablePlayers(players).ToList();
                if (availablePlayers.Count > 0)
                {
                    float se = GetSquidEnergy(availablePlayers);
                    int gameIndex = ChooseGame(se);
                    List<BigInteger> playersIds = GetPlayersIds(availablePlayers).ToList();
                    Thread.Sleep(15000);
                    bool claimResult = await Claim(gameIndex, playersIds);
                    players = await ParsePlayers();
                }
                Thread.Sleep(15000);
            }
        }
        public async Task<bool> Claim(int gameIndex, List<BigInteger> playersIds, int contractVersion=2)
        {
            var playGameFunction = new PlayGameFunction();
            playGameFunction.GameIndex = gameIndex;
            playGameFunction.PlayersId = playersIds;
            playGameFunction.ContractVersion = contractVersion;
            try
            {
                var playGameFunctionTxnReceipt = await gameContract.SendRequestAndWaitForReceiptAsync(playGameFunction);

                if (playGameFunctionTxnReceipt.Status.ToUlong() == 1)
                {
                    Logger.logAdd($"Successfully played a {gameIndex} game", ConsoleColor.Green);
                    return true;
                }
                else
                {
                    Logger.logAdd("Smth gone wrong while playing", ConsoleColor.Red);
                    return false;
                }
            }
            catch(Exception ex)
            {
                Logger.logAdd?.Invoke($"Exception while playing: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }
        public async Task<List<TokensViewFront>> ParsePlayers()
        {
            //var arrayUserPlayers = playersContract.GetFunction("arrayUserPlayers");
            //var result = await arrayUserPlayers.CallAsync<ArrayUserPlayersOutputDTOBase>(new ArrayUserPlayersFunction() { User = account.Address});
            try
            {
                var result = await playersContract.QueryDeserializingToObjectAsync<ArrayUserPlayersFunction, ArrayUserPlayersOutputDTO>(new ArrayUserPlayersFunction() { User = wallet.account.Address });
                return result.ReturnValue1;
            }
            catch(Exception ex)
            {
                Logger.logAdd?.Invoke($"Exeption while parsing players {ex.Message}", ConsoleColor.Red);
                Task.Delay(1000).Wait();
                return await ParsePlayers();
            }
        }
        public float GetSquidEnergy(List<TokensViewFront> players)
        {
            float se = 0;

            for (int i = 0; i < players.Count; i++)
            {
                se += (float)Web3.Convert.FromWei(players[i].SquidEnergy);
            }

            return se;
        }
        public IEnumerable<TokensViewFront> GetAvailablePlayers(List<TokensViewFront> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].ContractBought)
                {
                    if (CheckTime(players[i].BusyTo))
                    {
                        yield return players[i];
                    }
                }
                else
                {
                    Logger.logAdd?.Invoke($"Found player with expired contract", ConsoleColor.Red);
                }
            }
        }
        public IEnumerable<BigInteger> GetPlayersIds(List<TokensViewFront> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                yield return players[i].TokenId;
            }
        }
        public async Task<GetContractV2CostOutputDTO> GetContractV2Cost(List<BigInteger> playersIds, int contractIndex = 2)
        {
            var getContractV2CostFunction = new GetContractV2CostFunction();
            getContractV2CostFunction.PlayersId = playersIds;
            getContractV2CostFunction.ContractIndex = 30;
            var getContractV2CostOutputDTO = await gameContract.QueryDeserializingToObjectAsync<GetContractV2CostFunction, GetContractV2CostOutputDTO>(getContractV2CostFunction);

            return getContractV2CostOutputDTO;
        }
        public async Task<GetUserRewardBalancesOutputDTO> GetUserRewardBalances()
        {
            var getUserRewardBalancesFunction = new GetUserRewardBalancesFunction();
            getUserRewardBalancesFunction.User = wallet.account.Address;
            var getUserRewardBalancesOutputDTO = await gameContract.QueryDeserializingToObjectAsync<GetUserRewardBalancesFunction, GetUserRewardBalancesOutputDTO>(getUserRewardBalancesFunction);

            return getUserRewardBalancesOutputDTO;
        }
        public int ChooseGame(float se)
        {
            int gameIndex = ((int)se / 1000) % 7;

            if (gameIndex == 0)
            {
                gameIndex = 7;
            }

            return gameIndex-1;
        }
        public bool CheckTime(long time)
        {
            if (DateTimeOffset.Now.ToUnixTimeSeconds() >= time)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
