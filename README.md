# Ice Cream E-Commerce Platform

A comprehensive microservices-based e-commerce platform for ice cream products built with ASP.NET Core 7.

## Architecture Overview

│ Gateway │────▶│ Catalog │────▶│ SQL │
│ API │ │ Service │ │ Server │

│ │
│ │
▼ ▼

│ Cart │────▶│ Redis │ │ RabbitMQ │
│ Service │ │ Cache │ │ Broker │

│ ▲ ▲
│ │ │
▼ │ │

│ Checkout │────▶│ User │────▶│ SQL │
│ Service │ │ Service │ │ Server │


## Technology Stack

- **.NET 8** - Web API framework
- **SQL Server** - Primary database
- **Redis** - Caching and session management
- **RabbitMQ** - Message broker for async communication
- **Docker** - Containerization
- **YARP** - Reverse proxy for API Gateway
- **Entity Framework Core** - ORM
- **JWT** - Authentication
- **Swagger** - API documentation
- **xUnit** - Testing
- **AutoMapper** - Object mapping

## Services

### 1. **Catalog Service** (Port: 5001)
Manages ice cream products, flavors, and categories.
- Product CRUD operations
- Category management
- Inventory tracking
- Search functionality
- Stock management

### 2. **Cart Service** (Port: 5003)
Handles shopping cart operations with Redis caching.
- Add/remove items
- Update quantities
- Calculate totals
- Session-based carts

### 3. **Checkout Service** (Port: 5005)
Processes orders and payments.
- Order creation
- Payment processing
- Order history
- Invoice generation

### 4. **User Service** (Port: 5007)
Manages user accounts and authentication.
- Registration/Login
- JWT token generation
- Profile management
- Address management

### 5. **API Gateway** (Port: 5000)
Single entry point for all microservices.
- Request routing
- Authentication forwarding
- Rate limiting
- Request/Response logging

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/IceCreamECommerce.git
   cd IceCreamECommerce
