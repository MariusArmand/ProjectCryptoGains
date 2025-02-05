using System.ComponentModel;

namespace ProjectCryptoGains
{
    public static partial class Models
    {
        public partial class AssetsModel : INotifyPropertyChanged
        {
            private string? _code;
            private string? _asset;

            public string? Code
            {
                get => _code;
                set
                {
                    if (_code != value)
                    {
                        _code = value;
                        OnPropertyChanged(nameof(Code));
                    }
                }
            }

            public string? Asset
            {
                get => _asset;
                set
                {
                    if (_asset != value)
                    {
                        _asset = value;
                        OnPropertyChanged(nameof(Asset));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public partial class KrakenAssetsModel : INotifyPropertyChanged
        {
            private string? _code;
            private string? _asset;

            public string? Code
            {
                get => _code;
                set
                {
                    if (_code != value)
                    {
                        _code = value;
                        OnPropertyChanged(nameof(Code));
                    }
                }
            }

            public string? Asset
            {
                get => _asset;
                set
                {
                    if (_asset != value)
                    {
                        _asset = value;
                        OnPropertyChanged(nameof(Asset));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public partial class KrakenPairsModel : INotifyPropertyChanged
        {
            private string? _code;
            private string? _asset_left;
            private string? _asset_right;

            public string? Code
            {
                get => _code;
                set
                {
                    if (_code != value)
                    {
                        _code = value;
                        OnPropertyChanged(nameof(Code));
                    }
                }
            }

            public string? Asset_left
            {
                get => _asset_left;
                set
                {
                    if (_asset_left != value)
                    {
                        _asset_left = value;
                        OnPropertyChanged(nameof(Asset_left));
                    }
                }
            }

            public string? Asset_right
            {
                get => _asset_right;
                set
                {
                    if (_asset_right != value)
                    {
                        _asset_right = value;
                        OnPropertyChanged(nameof(Asset_right));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class ManualTransactionsModel
        {
            public int RowNumber { get; set; }
            public string? Refid { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Exchange { get; set; }
            public string? Asset { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Fee { get; set; }
            public string? Source { get; set; }
            public string? Target { get; set; }
            public string? Notes { get; set; }
        }

        public class KrakenLedgersModel
        {
            public int RowNumber { get; set; }
            public string? Txid { get; set; }
            public string? Refid { get; set; }
            public string? Time { get; set; }
            public string? Type { get; set; }
            public string? Subtype { get; set; }
            public string? Aclass { get; set; }
            public string? Asset { get; set; }
            public string? Wallet { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Fee { get; set; }
            public decimal? Balance { get; set; }
        }

        public class KrakenTradesModel
        {
            public int RowNumber { get; set; }
            public string? Txid { get; set; }
            public string? Ordertxid { get; set; }
            public string? Pair { get; set; }
            public string? Time { get; set; }
            public string? Type { get; set; }
            public string? Ordertype { get; set; }
            public decimal? Price { get; set; }
            public decimal? Cost { get; set; }
            public decimal? Fee { get; set; }
            public decimal? Vol { get; set; }
            public decimal? Margin { get; set; }
            public string? Misc { get; set; }
            public string? Ledgers { get; set; }
        }

        public class LedgersModel
        {
            public int RowNumber { get; set; }
            public string? Refid { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Exchange { get; set; }
            public decimal? Amount { get; set; }
            public string? Currency { get; set; }
            public decimal? Fee { get; set; }
            public string? Balance { get; set; }
            public string? Source { get; set; }
            public string? Target { get; set; }
            public string? Notes { get; set; }
        }

        public class RewardsModel
        {
            public int RowNumber { get; set; }
            public string? Refid { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Exchange { get; set; }
            public string? Currency { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Amount_fiat { get; set; }
            public decimal? Tax { get; set; }
            public decimal? Unit_price { get; set; }
            public decimal? Unit_price_break_even { get; set; }
            public decimal? Amount_sell_break_even { get; set; }
            public string? Notes { get; set; }
        }

        public class TradesRawModel
        {
            public int RowNumber { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Exchange { get; set; }
            public decimal? Base_amount { get; set; }
            public string? Base_currency { get; set; }
            public decimal? Quote_amount { get; set; }
            public string? Quote_currency { get; set; }
            public decimal? Fee { get; set; }
            //public string? Fee_currency { get; set; }
        }

        public class TradesModel
        {
            public int RowNumber { get; set; }
            public string? Refid { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Exchange { get; set; }
            public string? Base_currency { get; set; }
            public decimal? Base_amount { get; set; }
            public decimal? Base_fee { get; set; }
            public decimal? Base_fee_fiat { get; set; }
            public string? Quote_currency { get; set; }
            public decimal? Quote_amount { get; set; }
            public decimal? Quote_amount_fiat { get; set; }
            public decimal? Quote_fee { get; set; }
            public decimal? Quote_fee_fiat { get; set; }
            public decimal? Base_unit_price { get; set; }
            public decimal? Base_unit_price_fiat { get; set; }
            public decimal? Quote_unit_price { get; set; }
            public decimal? Quote_unit_price_fiat { get; set; }
            public decimal? Total_fee_fiat { get; set; }
            public decimal? Costs_proceeds { get; set; }
        }
        public class GainsModel
        {
            public int RowNumber { get; set; }
            public string? Refid { get; set; }
            public string? Date { get; set; }
            public string? Type { get; set; }
            public string? Base_currency { get; set; }
            public decimal? Base_amount { get; set; }
            public string? Quote_currency { get; set; }
            public decimal? Quote_amount { get; set; }
            public decimal? Base_unit_price_fiat { get; set; }
            public decimal? Costs_proceeds { get; set; }
            public decimal? Tx_balance_remaining { get; set; }
            public decimal? Gain { get; set; }
        }

        public class GainsSummaryModel
        {
            public int RowNumber { get; set; }
            public string? Currency { get; set; }
            public decimal? Gain { get; set; }
        }

        public class RewardsSummaryModel
        {
            public int RowNumber { get; set; }
            public string? Currency { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Amount_fiat { get; set; }
            public decimal? Tax { get; set; }
            public decimal? Unit_price { get; set; }
            public decimal? Unit_price_break_even { get; set; }
            public decimal? Amount_sell_break_even { get; set; }
        }

        public class AvgBuyPriceModel
        {
            public int RowNumber { get; set; }
            public string? Currency { get; set; }
            public decimal? Amount_fiat { get; set; }
        }

        public class BalancesModel
        {
            public int RowNumber { get; set; }
            public string? Currency { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Amount_fiat { get; set; }
        }

        public class TransactionsModel
        {
            public required string RefId { get; set; }
            public string? Date { get; set; }
            public required decimal Amount { get; set; }
            public decimal? Unit_price { get; set; }
            public decimal? Costs_Proceeds { get; set; }
            public decimal? Tx_Balance_Remaining { get; set; }
            public decimal? Gain { get; set; }
        }
    }
}