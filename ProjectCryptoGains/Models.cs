using System;

namespace ProjectCryptoGains
{
    public static partial class Models
    {
        public class AssetCatalogModel
        {
            public required string Code { get; set; }
            public required string Asset { get; set; }
        }

        public class AssetCodesKrakenModel
        {
            public required string Code { get; set; }
            public required string Asset { get; set; }
        }

        public class AvgBuyPriceModel
        {
            public required int RowNumber { get; set; }
            public required string Currency { get; set; }
            public required decimal Amount_fiat { get; set; }
        }

        public class BalancesModel
        {
            public required int RowNumber { get; set; }
            public required string Currency { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Amount_fiat { get; set; }
        }

        public class GainsModel
        {
            public required int RowNumber { get; set; }
            public required string Refid { get; set; }
            public required DateTime Date { get; set; }
            public required string Type { get; set; }
            public required string Base_currency { get; set; }
            public required decimal Base_amount { get; set; }
            public required string Quote_currency { get; set; }
            public required decimal Quote_amount { get; set; }
            public required decimal Base_unit_price_fiat { get; set; }
            public required decimal Costs_proceeds { get; set; }
            public decimal? Tx_balance_remaining { get; set; }
            public decimal? Gain { get; set; }
        }

        public class TransactionsModel
        {
            public required string RefId { get; set; }
            public required DateTime Date { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Unit_price { get; set; }
            public required decimal Costs_Proceeds { get; set; }
            public decimal? Tx_Balance_Remaining { get; set; }
            public decimal? Gain { get; set; }
        }

        public class GainsSummaryModel
        {
            public required int RowNumber { get; set; }
            public required string Currency { get; set; }
            public required decimal Gain { get; set; }
        }

        public class LedgersKrakenModel
        {
            public required int RowNumber { get; set; }
            public required string Txid { get; set; }
            public required string Refid { get; set; }
            public required DateTime Time { get; set; }
            public required string Type { get; set; }
            public required string Subtype { get; set; }
            public required string Aclass { get; set; }
            public required string Asset { get; set; }
            public required string Wallet { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Fee { get; set; }
            public required decimal Balance { get; set; }
        }

        public class LedgersManualModel
        {
            public required int RowNumber { get; set; }
            public required string Refid { get; set; }
            public required DateTime Date { get; set; }
            public required string Type { get; set; }
            public string? Exchange { get; set; }
            public required string Asset { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Fee { get; set; }
            public string? Source { get; set; }
            public string? Target { get; set; }
            public string? Notes { get; set; }
        }

        public class LedgersModel
        {
            public required int RowNumber { get; set; }
            public required string Refid { get; set; }
            public required DateTime Date { get; set; }
            public required string Type { get; set; }
            public required string Exchange { get; set; }
            public required decimal Amount { get; set; }
            public required string Currency { get; set; }
            public required decimal Fee { get; set; }
            public required string Source { get; set; }
            public required string Target { get; set; }
            public required string Notes { get; set; }
        }

        public class RewardsModel
        {
            public required int RowNumber { get; set; }
            public required string Refid { get; set; }
            public required DateTime Date { get; set; }
            public required string Type { get; set; }
            public required string Exchange { get; set; }
            public required string Currency { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Amount_fiat { get; set; }
            public required decimal Tax { get; set; }
            public required decimal Unit_price { get; set; }
            public required decimal Unit_price_break_even { get; set; }
            public required decimal Amount_sell_break_even { get; set; }
        }

        public class RewardsSummaryModel
        {
            public required int RowNumber { get; set; }
            public required string Currency { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Amount_fiat { get; set; }
            public required decimal Tax { get; set; }
            public required decimal Unit_price { get; set; }
            public required decimal Unit_price_break_even { get; set; }
            public required decimal Amount_sell_break_even { get; set; }
        }

        public class MetricsRewardsSummaryModel
        {
            public required int RowNumber { get; set; }
            public required string Currency { get; set; }
            public required decimal Amount { get; set; }
            public required decimal Amount_fiat { get; set; }
        }

        public class TradesModel
        {
            public required int RowNumber { get; set; }
            public required string Refid { get; set; }
            public required DateTime Date { get; set; }
            public required string Type { get; set; }
            public required string Exchange { get; set; }
            public required string Base_currency { get; set; }
            public required decimal Base_amount { get; set; }
            public required decimal Base_fee { get; set; }
            public decimal? Base_fee_fiat { get; set; }
            public required string Quote_currency { get; set; }
            public required decimal Quote_amount { get; set; }
            public decimal? Quote_amount_fiat { get; set; }
            public required decimal Quote_fee { get; set; }
            public decimal? Quote_fee_fiat { get; set; }
            public required decimal Base_unit_price { get; set; }
            public decimal? Base_unit_price_fiat { get; set; }
            public required decimal Quote_unit_price { get; set; }
            public decimal? Quote_unit_price_fiat { get; set; }
            public decimal? Total_fee_fiat { get; set; }
            public decimal? Costs_proceeds { get; set; }
        }
    }
}