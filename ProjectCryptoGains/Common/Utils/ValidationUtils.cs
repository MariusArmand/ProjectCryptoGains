using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using static ProjectCryptoGains.Common.Utils.Utils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class ValidationUtils
    {
        public static List<string> MissingAssets(FbConnection connection)
        {
            List<string> missingAssets = [];

            using DbCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT DISTINCT ledgers_kraken.ASSET
                                          FROM TB_LEDGERS_KRAKEN_S ledgers_kraken
                                              LEFT OUTER JOIN TB_ASSET_CODES_KRAKEN_S assets_kraken
                                                  ON ledgers_kraken.ASSET = assets_kraken.CODE
                                          WHERE assets_kraken.ASSET IS NULL";

            using (DbDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        missingAssets.Add(asset);
                    }
                }
            }

            return missingAssets;
        }

        public static List<string> MalconfiguredAssets(FbConnection connection)
        {
            List<string> malfconfiguredAsset = [];

            using DbCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT assets_kraken.CODE 
                                          FROM TB_ASSET_CODES_KRAKEN_S assets_kraken
									          LEFT OUTER JOIN TB_ASSET_CATALOG_S catalog
									              ON assets_kraken.ASSET = catalog.ASSET
									      WHERE catalog.CODE IS NULL";

            using (DbDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string code)
                    {
                        malfconfiguredAsset.Add(code);
                    }
                }
            }

            return malfconfiguredAsset;
        }

        public static List<string> MissingAssetsManual(FbConnection connection)
        {
            List<string> missingAssets = [];

            using DbCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT DISTINCT ledgers_manual.ASSET 
                                          FROM TB_LEDGERS_MANUAL_S ledgers_manual
                                              LEFT OUTER JOIN TB_ASSET_CATALOG_S catalog
                                                  ON ledgers_manual.ASSET = catalog.ASSET
                                          WHERE catalog.CODE IS NULL";

            using (DbDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        missingAssets.Add(asset);
                    }
                }
            }

            return missingAssets;
        }

        public static List<(string RefId, string Type)> UnsupportedTypes(FbConnection connection, LedgerSource ledgerSource)
        {
            List<(string RefId, string Type)> unsupportedTypes = [];

            using DbCommand selectCommand = connection.CreateCommand();

            switch (ledgerSource)
            {
                case LedgerSource.Kraken:
                    selectCommand.CommandText = @"SELECT REFID, TYPE
                                                  FROM TB_LEDGERS_KRAKEN_S
                                                  WHERE UPPER(TYPE) NOT IN ('DEPOSIT', 'WITHDRAWAL', 'TRADE', 'SPEND', 'RECEIVE', 'STAKING', 'EARN', 'TRANSFER')";
                    break;

                case LedgerSource.Manual:
                    selectCommand.CommandText = @"SELECT REFID, TYPE
                                                  FROM TB_LEDGERS_MANUAL_S
                                                  WHERE TYPE NOT IN ('DEPOSIT', 'WITHDRAWAL', 'TRADE', 'STAKING', 'AIRDROP')";
                    break;

                default:
                    throw new ArgumentException($"Unsupported ledger source: {ledgerSource}.");
            }

            using (DbDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string refid && reader[1] is string type)
                    {
                        unsupportedTypes.Add((refid, type));
                    }
                }
            }

            return unsupportedTypes;
        }
    }
}