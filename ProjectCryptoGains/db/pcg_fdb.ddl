/******************************************************************************/
/*                                  Database                                  */
/******************************************************************************/

CREATE DATABASE 'pcg.fdb'
PAGE_SIZE 16384
DEFAULT CHARACTER SET UTF8 COLLATION UTF8;

CONNECT 'pcg.fdb';

SET SQL DIALECT 3;
SET NAMES UTF8;

/******************************************************************************/
/*                                   Tables                                   */
/******************************************************************************/

CREATE TABLE TB_ASSET_CATALOG (
    ASSET   VARCHAR(20) NOT NULL,
    LABEL   VARCHAR(50) NOT NULL
);

CREATE TABLE TB_ASSETS_KRAKEN (
    ASSET   VARCHAR(20) NOT NULL,
    LABEL   VARCHAR(50) NOT NULL
);

CREATE TABLE TB_AVG_BUY_PRICE (
    ASSET	     VARCHAR(20) NOT NULL,
    AMOUNT_FIAT  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_BALANCES (
    ASSET        VARCHAR(20) NOT NULL,
    AMOUNT       DECIMAL(20,10) NOT NULL,
    AMOUNT_FIAT  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_EXCHANGE_RATES (
    "DATE"         TIMESTAMP NOT NULL,
    ASSET          VARCHAR(20) NOT NULL,
    FIAT_CURRENCY  VARCHAR(50) NOT NULL,
    EXCHANGE_RATE  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_GAINS (
    REFID                 VARCHAR(100) NOT NULL,
    TX_BALANCE_REMAINING  DECIMAL(20,10),
    GAIN                  DECIMAL(20,10)
);

CREATE TABLE TB_LEDGERS_KRAKEN (
    TXID     VARCHAR(100) NOT NULL,
    REFID    VARCHAR(100) NOT NULL,
    "TIME"   TIMESTAMP NOT NULL,
    "TYPE"   VARCHAR(50) NOT NULL,
    SUBTYPE  VARCHAR(50) NOT NULL,
    ACLASS   VARCHAR(50) NOT NULL,
    ASSET    VARCHAR(20) NOT NULL,
    WALLET   VARCHAR(50) NOT NULL,
    AMOUNT   DECIMAL(20,10) NOT NULL,
    FEE      DECIMAL(20,10) NOT NULL,
    BALANCE  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_LEDGERS_MANUAL (
    REFID     VARCHAR(100) NOT NULL,
    "DATE"    TIMESTAMP NOT NULL,
    "TYPE"    VARCHAR(50) NOT NULL,
    EXCHANGE  VARCHAR(50),
    ASSET     VARCHAR(20) NOT NULL,
    AMOUNT    DECIMAL(20,10) NOT NULL,
    FEE       DECIMAL(20,10) NOT NULL,
    SOURCE    VARCHAR(50),
    TARGET    VARCHAR(50),
    NOTES     VARCHAR(200)
);

CREATE TABLE TB_LEDGERS (
    REFID        VARCHAR(100) NOT NULL,
    "DATE"       TIMESTAMP NOT NULL,
    TYPE_SOURCE  VARCHAR(50) NOT NULL,
    "TYPE"       VARCHAR(50) NOT NULL,
    EXCHANGE     VARCHAR(50) NOT NULL,
    AMOUNT       DECIMAL(20,10) NOT NULL,
    ASSET        VARCHAR(20) NOT NULL,
    FEE          DECIMAL(20,10) NOT NULL,
    SOURCE       VARCHAR(50) NOT NULL,
    TARGET       VARCHAR(50) NOT NULL,
    NOTES        VARCHAR(200) NOT NULL
);

CREATE TABLE TB_METRICS (
    METRIC   VARCHAR(50) NOT NULL,
    "VALUE"  VARCHAR(50) NOT NULL
);

CREATE TABLE TB_REWARDS (
    REFID                   VARCHAR(100) NOT NULL,
    "DATE"                  TIMESTAMP NOT NULL,
    "TYPE"                  VARCHAR(50) NOT NULL,
    EXCHANGE                VARCHAR(50) NOT NULL,
    ASSET                   VARCHAR(20) NOT NULL,
    AMOUNT                  DECIMAL(20,10) NOT NULL,
    AMOUNT_FIAT             DECIMAL(20,10) NOT NULL,
    TAX                     DECIMAL(20,10) NOT NULL,
    UNIT_PRICE              DECIMAL(20,10) NOT NULL,
    UNIT_PRICE_BREAK_EVEN   DECIMAL(20,10) NOT NULL,
    AMOUNT_SELL_BREAK_EVEN  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_REWARDS_SUMMARY (
    ASSET        VARCHAR(20) NOT NULL,
    AMOUNT       DECIMAL(20,10) NOT NULL,
    AMOUNT_FIAT  DECIMAL(20,10) NOT NULL
);

CREATE TABLE TB_SETTINGS (
    NAME     VARCHAR(50) NOT NULL,
    "VALUE"  VARCHAR(200)
);

CREATE TABLE TB_TRADES (
    REFID                  VARCHAR(100) NOT NULL,
    "DATE"                 TIMESTAMP NOT NULL,
    "TYPE"                 VARCHAR(50) NOT NULL,
    EXCHANGE               VARCHAR(50) NOT NULL,
    BASE_ASSET             VARCHAR(20) NOT NULL,
    BASE_AMOUNT            DECIMAL(20,10) NOT NULL,
    BASE_FEE               DECIMAL(20,10) NOT NULL,
    BASE_FEE_FIAT          DECIMAL(20,10),
    QUOTE_ASSET            VARCHAR(20) NOT NULL,
    QUOTE_AMOUNT           DECIMAL(20,10) NOT NULL,
    QUOTE_AMOUNT_FIAT      DECIMAL(20,10),
    QUOTE_FEE              DECIMAL(20,10) NOT NULL,
    QUOTE_FEE_FIAT         DECIMAL(20,10),
    BASE_UNIT_PRICE        DECIMAL(20,10) NOT NULL,
    BASE_UNIT_PRICE_FIAT   DECIMAL(20,10),
    QUOTE_UNIT_PRICE       DECIMAL(20,10) NOT NULL,
    QUOTE_UNIT_PRICE_FIAT  DECIMAL(20,10),
    TOTAL_FEE_FIAT         DECIMAL(20,10),
    COSTS_PROCEEDS         DECIMAL(20,10)
);

/******************************************************************************/
/*                                  Indexes                                   */
/******************************************************************************/

CREATE UNIQUE INDEX UIX_ASSET_CATALOG_ASSET_NOCASE ON TB_ASSET_CATALOG COMPUTED BY (UPPER(ASSET));
CREATE UNIQUE INDEX UIX_ASSET_CATALOG_LABEL_NOCASE ON TB_ASSET_CATALOG COMPUTED BY (UPPER(LABEL));
CREATE UNIQUE INDEX UIX_ASSETS_KRAKEN_ASSET_NOCASE ON TB_ASSETS_KRAKEN COMPUTED BY (UPPER(ASSET));
CREATE UNIQUE INDEX UIX_EXCHANGE_RATES_COMB ON TB_EXCHANGE_RATES ("DATE", ASSET, FIAT_CURRENCY, EXCHANGE_RATE);
CREATE INDEX IX_LEDGERS_ASSET ON TB_LEDGERS (ASSET);
CREATE UNIQUE INDEX UIX_SETTINGS_NAME_NOCASE ON TB_SETTINGS COMPUTED BY (UPPER(NAME));

/******************************************************************************/
/*                                   Inserts                                  */
/******************************************************************************/

INSERT INTO TB_SETTINGS (NAME, "VALUE") VALUES ('FIAT_CURRENCY', 'EUR');
INSERT INTO TB_SETTINGS (NAME, "VALUE") VALUES ('REWARDS_TAX_PERCENTAGE', '30');
INSERT INTO TB_SETTINGS (NAME, "VALUE") VALUES ('COINDESKDATA_API_KEY', '');
INSERT INTO TB_SETTINGS (NAME, "VALUE") VALUES ('PRINTOUT_TITLE_PREFIX', '');

INSERT INTO TB_ASSET_CATALOG (ASSET, LABEL) VALUES ('EUR', 'Euro');
INSERT INTO TB_ASSET_CATALOG (ASSET, LABEL) VALUES ('BTC', 'Bitcoin');

INSERT INTO TB_ASSETS_KRAKEN (ASSET, LABEL) VALUES ('EUR', 'Euro');
INSERT INTO TB_ASSETS_KRAKEN (ASSET, LABEL) VALUES ('BTC', 'Bitcoin');