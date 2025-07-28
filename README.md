# FinVault — Personal Banking API

A RESTful banking API built with ASP.NET Core 8. Supports multi-account management, deposits, withdrawals, transfers, transaction history, monthly statements, spending analytics, fraud detection, and in-app notifications.

## Tech Stack

- **ASP.NET Core 8** — Web API
- **Entity Framework Core 8** — SQLite
- **JWT Bearer** — authentication
- **BCrypt.Net** — password hashing
- **Swagger/OpenAPI** — interactive docs
- **xUnit + Moq** — unit tests

## Getting Started

```bash
cd FinVault.API
dotnet run
```

Swagger UI available at `http://localhost:5000/swagger`

## Running Tests

```bash
dotnet test FinVault.Tests/FinVault.Tests.csproj
```

## API Endpoints

### Auth
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login, returns JWT |
| POST | `/api/auth/logout` | Logout (client-side) |

### Users
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Get profile |
| PUT | `/api/users/me` | Update profile |
| PUT | `/api/users/me/password` | Change password |
| DELETE | `/api/users/me` | Deactivate account |

### Accounts
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/accounts` | Create account (checking/savings) |
| GET | `/api/accounts` | List all accounts |
| GET | `/api/accounts/{id}` | Get account details |
| PUT | `/api/accounts/{id}/freeze` | Freeze / unfreeze |
| DELETE | `/api/accounts/{id}` | Close account |
| GET | `/api/accounts/{id}/statement?year=2025&month=7` | Monthly statement |

### Transactions
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/accounts/{id}/deposit` | Deposit funds |
| POST | `/api/accounts/{id}/withdraw` | Withdraw funds |
| POST | `/api/transfers` | Transfer between accounts |
| GET | `/api/accounts/{id}/transactions` | List with filters + pagination |
| GET | `/api/transactions/{id}` | Single transaction |
| GET | `/api/accounts/{id}/analytics?year=2025&month=7` | Spending by category |

### Notifications
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/notifications` | Get notifications |
| PUT | `/api/notifications/{id}/read` | Mark one as read |
| PUT | `/api/notifications/read-all` | Mark all as read |
| DELETE | `/api/notifications/{id}` | Delete notification |

## Features

- **JWT auth** with 24-hour expiry
- **Fraud detection** — transactions over $10,000 are flagged automatically
- **Spending analytics** — monthly breakdown by category
- **Transaction filters** — by type, category, date range, amount, flagged status
- **Pagination** on transaction history
- **Soft delete** on user accounts
- **Account freeze/unfreeze**
- **Auto notifications** on every transaction
