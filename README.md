# Titan Exchange Core

Titan is a **high-performance, centralized financial exchange engine** designed to facilitate real-time trade matching with high precision and strict data integrity. The system is engineered to handle high-frequency order ingestion, maintain a deterministic order book state, and provide an immutable audit trail for all transactions.

---

## 1. Project Overview
The Titan Exchange Core provides the infrastructure for a scalable electronic marketplace. It bridges the gap between high-traffic user ingestion via a web gateway and a low-latency execution core. 

The system ensures that every trade is executed according to the strict rules of **price-time priority** and that all market participants are treated fairly through a deterministic matching algorithm.

---

## 2. System Architecture
The application is composed of three primary architectural layers:

* **Ingestion Gateway:** An ASP.NET Core-based interface that serves as the entry point for all market participants. It handles request validation, security, and the handoff of trade intent to the matching core.
* **The Matching Engine:** The stateful heart of the system. It maintains an in-memory representation of the **Limit Order Book (LOB)** and executes the core matching logic.
* **Persistence Layer:** A durable storage system that records every executed trade and order state change for regulatory compliance and audit purposes.

---

## 3. Functional Specifications

### Order Management
The engine supports the lifecycle of two core order types:

1.  **Limit Orders:** Orders to buy or sell a specific quantity at a specified price or better. These orders provide liquidity to the market and are stored in the order book until matched or cancelled.
2.  **Market Orders:** Aggressive orders designed for immediate execution at the best available current price. These orders consume liquidity from the book.

### Matching Algorithm (FIFO)
Titan utilizes a **Price-Time Priority (FIFO)** matching algorithm. The engine ensures that:
* **Bids (Buy Orders)** are prioritized by the highest price.
* **Asks (Sell Orders)** are prioritized by the lowest price.
* In the event of equal prices, the order that arrived first in the system receives priority for execution.

### Trade Settlement
When the matching conditions are met ($Bid Price \ge Ask Price$), the engine generates a **Trade** record. The system handles **Partial Fills**, where a single large order may be matched against multiple smaller resting orders until its quantity is fully satisfied.

---

## 4. Technical Requirements & Standards

| Category | Requirement |
| :--- | :--- |
| **Language & Runtime** | .NET 10.0 / C# |
| **Thread Safety** | Strict synchronization primitives to ensure Order Book integrity. |
| **Precision** | 128-bit decimal precision for all financial calculations. |
| **Observability** | Real-time metrics on throughput (OPS) and execution latency. |

---
