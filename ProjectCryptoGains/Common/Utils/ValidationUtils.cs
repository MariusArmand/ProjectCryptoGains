﻿using FirebirdSql.Data.FirebirdClient;
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
                                          FROM TB_LEDGERS_KRAKEN ledgers_kraken
                                              LEFT OUTER JOIN TB_ASSETS_KRAKEN assets_kraken
                                                  ON ledgers_kraken.ASSET = assets_kraken.ASSET
                                          WHERE assets_kraken.LABEL IS NULL";

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
            selectCommand.CommandText = @"SELECT assets_kraken.ASSET 
                                          FROM TB_ASSETS_KRAKEN assets_kraken
									          LEFT OUTER JOIN TB_ASSET_CATALOG catalog
									              ON assets_kraken.LABEL = catalog.LABEL
									      WHERE catalog.ASSET IS NULL";

            using (DbDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        malfconfiguredAsset.Add(asset);
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
                                          FROM TB_LEDGERS_MANUAL ledgers_manual
                                          LEFT OUTER JOIN TB_ASSET_CATALOG catalog
                                              ON ledgers_manual.ASSET = catalog.ASSET
                                          WHERE catalog.ASSET IS NULL";

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
                                                  FROM TB_LEDGERS_KRAKEN
                                                  WHERE UPPER(TYPE) NOT IN ('DEPOSIT', 'WITHDRAWAL', 'TRADE', 'SPEND', 'RECEIVE', 'STAKING', 'EARN', 'TRANSFER')";
                    break;

                case LedgerSource.Manual:
                    selectCommand.CommandText = @"SELECT REFID, TYPE
                                                  FROM TB_LEDGERS_MANUAL
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