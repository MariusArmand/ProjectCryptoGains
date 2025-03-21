# ProjectCryptoGains

[![Project Status: In Development](https://img.shields.io/badge/status-in%20development-yellow.svg)](https://github.com/MariusArmand/ProjectCryptoGains)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## Overview
Project Crypto Gains(PCG) is a tool designed to simplify tax management for cryptocurrency investments;  
It offers a user-friendly solution for tracking crypto transactions and calculating taxes using the LIFO (Last In, First Out) method

## Features

### Transaction Types Supported
- **Simple Limit/Market Buys/Sells**
- **Rewards**
- **Deposits**
- **Withdraws**

### CSV Import & Processing
- Load ledger data from Kraken FX in CSV format
- Process manually created ledger CSV files

### Transaction Handling
- Calculates costs and proceeds from crypto transactions
- Computes gains using the LIFO method
- Manages balance calculations

### Metrics
- Track total invested and last invested
- Calculates the average buy price per asset
- Shows the total rewards gathered and their conversion to fiat

### Rewards
- Calculates rewards from staking, converted to their fiat price upon time of receiving, which can be used for withholding tax preparation

### Data Storage
- Locally stores previously looked-up price data to reduce API calls and improve performance

### Printable
- All relevant output is printable to provide to your accountant

## Requirements
- **API Access**: Users must have one of the [CoinDesk Data](https://developers.coindesk.com/pricing/) API licenses tailored to their needs for real-time price data

**Note**: Optimized and tested for Euro (EUR); USD support is available but requires thorough testing for accuracy

## Usage

### Data Import
- Import your ledgers from Kraken FX and/or import manual ledgers

### Execution
Let PCG process your data to generate the following reports:
- Ledgers
- Trades
- Gains 
- Rewards
- Balances
- Metrics

### Review Results
- Check all output for accuracy

## Installation
**Download the ProjectCryptoGains ZIP file**  
Go to the Releases(https://github.com/MariusArmand/ProjectCryptoGains/releases) page and download ProjectCryptoGains.zip

**Extract the ZIP file**  
Unzip the downloaded ProjectCryptoGains.zip to your desired location using your preferred extraction tool

**Run the Application**  
Locate the extracted folder and double-click ProjectCryptoGains.exe to launch the application

## Disclaimer
**Accuracy**: While efforts have been made to aim for accuracy, the correctness of the calculations should be verified by a professional accountant

**Historical Price Data Timing**: Historical prices used in the tool are fetched from [CoinDesk Data] using the opening price on the relevant date;  
This approach limits API calls and maintains performance, with the assumption that consistently using opening prices provides a balanced representation 
of historical price data

**General Disclaimer**: Use at your own risk; This software is provided 'as is', without warranty of any kind, either express or implied, 
including but not limited to the warranties of merchantability, fitness for a particular purpose, and non-infringement

**None of the output from this program should be considered financial advice; it is for educational purposes only;** In no event shall the author or 
contributors be liable for any claim, damages, or other liability, whether in an action of contract, tort, or otherwise, arising from, out of, 
or in connection with the software or the use or other dealings in the software

## Contact
For any questions or feedback, feel free to reach out via:
- The **issues page** of this repository
- My X handle: [@ProjCryptoGains](https://x.com/ProjCryptoGains).

## Licensing

ProjectCryptoGains is licensed under the [Apache License 2.0](LICENSE).

This project includes or depends on the following third-party components:
- **Firebird Embedded**:
  - [Initial Developer's Public License (IDPL)](licenses/LICENSE-FIREBIRD-IDPL.txt)
  - [InterBase Public License (IPL)](licenses/LICENSE-FIREBIRD-IPL.txt)
- **ControlzEx**:
  - [MIT License](licenses/LICENSE-CONTROLZEX-MIT.txt)

If you distribute this project with Firebird Embedded, please ensure compliance with the [IDPL](licenses/LICENSE-FIREBIRD-IDPL.txt) and [IPL](licenses/LICENSE-FIREBIRD-IPL.txt) terms.