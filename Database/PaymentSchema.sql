-- =============================================
-- EPS Payment Gateway Database Schema
-- Database: RealProxyDB
-- Version: 1.0
-- =============================================

-- =============================================
-- Table: payments
-- Main payment transaction records
-- =============================================
CREATE TABLE IF NOT EXISTS `payments` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    
    -- Transaction Identifiers
    `CustomerOrderId` VARCHAR(100) NOT NULL,
    `MerchantTransactionId` VARCHAR(100) NOT NULL,
    `EpsTransactionId` VARCHAR(100) NULL,
    
    -- Transaction Details
    `Amount` DECIMAL(18, 2) NOT NULL,
    `Currency` VARCHAR(10) NOT NULL DEFAULT 'BDT',
    `TransactionTypeId` INT NOT NULL DEFAULT 1 COMMENT '1=Web, 2=Android, 3=IOS',
    
    -- Payment Status
    `Status` VARCHAR(50) NOT NULL DEFAULT 'Pending' COMMENT 'Pending, Success, Failed, Cancelled, Expired',
    `PaymentMethod` VARCHAR(100) NULL COMMENT 'OKWallet, bKash, Nagad, etc.',
    
    -- Product Information
    `ProductName` VARCHAR(255) NOT NULL,
    `ProductProfile` VARCHAR(100) NULL,
    `ProductCategory` VARCHAR(100) NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    
    -- Customer Information
    `CustomerName` VARCHAR(255) NOT NULL,
    `CustomerEmail` VARCHAR(255) NOT NULL,
    `CustomerPhone` VARCHAR(50) NOT NULL,
    `CustomerAddress` VARCHAR(500) NULL,
    `CustomerCity` VARCHAR(100) NULL,
    `CustomerState` VARCHAR(100) NULL,
    `CustomerPostcode` VARCHAR(20) NULL,
    `CustomerCountry` VARCHAR(10) NULL,
    
    -- URLs
    `SuccessUrl` VARCHAR(500) NULL,
    `FailUrl` VARCHAR(500) NULL,
    `CancelUrl` VARCHAR(500) NULL,
    
    -- Security & Verification
    `IpAddress` VARCHAR(100) NULL,
    `UserAgent` VARCHAR(500) NULL,
    `VerificationHash` VARCHAR(255) NULL COMMENT 'Hash for additional security verification',
    `VerifiedAt` DATETIME NULL,
    `VerificationAttempts` INT NOT NULL DEFAULT 0,
    
    -- Timestamps
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `CompletedAt` DATETIME NULL,
    `ExpiresAt` DATETIME NULL,
    
    -- Additional Info
    `ErrorCode` VARCHAR(50) NULL,
    `ErrorMessage` TEXT NULL,
    `Notes` TEXT NULL,
    
    -- Foreign Key
    CONSTRAINT `FK_Payment_User` FOREIGN KEY (`UserId`) REFERENCES `users`(`Id`) ON DELETE CASCADE,
    
    -- Indexes for Performance
    INDEX `IDX_Payment_UserId` (`UserId`),
    INDEX `IDX_Payment_Status` (`Status`),
    INDEX `IDX_Payment_CreatedAt` (`CreatedAt` DESC),
    INDEX `IDX_Payment_CustomerEmail` (`CustomerEmail`),
    
    -- Unique Constraints
    UNIQUE INDEX `UNQ_Payment_CustomerOrderId` (`CustomerOrderId`),
    UNIQUE INDEX `UNQ_Payment_MerchantTransactionId` (`MerchantTransactionId`),
    UNIQUE INDEX `UNQ_Payment_EpsTransactionId` (`EpsTransactionId`),
    
    -- Composite Indexes for Common Queries
    INDEX `IDX_Payment_UserId_Status_CreatedAt` (`UserId`, `Status`, `CreatedAt` DESC),
    INDEX `IDX_Payment_Status_CreatedAt` (`Status`, `CreatedAt` DESC)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Main payment transactions table';

-- =============================================
-- Table: payment_logs
-- Audit trail and activity log
-- =============================================
CREATE TABLE IF NOT EXISTS `payment_logs` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `PaymentId` INT NOT NULL,
    
    -- Log Details
    `Action` VARCHAR(100) NOT NULL COMMENT 'Initialize, Callback, Verify, StatusChange, etc.',
    `PreviousStatus` VARCHAR(50) NULL,
    `NewStatus` VARCHAR(50) NULL,
    
    -- Request/Response Data
    `RequestData` TEXT NULL COMMENT 'JSON of request',
    `ResponseData` TEXT NULL COMMENT 'JSON of response',
    `ErrorMessage` TEXT NULL,
    
    -- Context
    `IpAddress` VARCHAR(100) NULL,
    `UserAgent` VARCHAR(500) NULL,
    
    -- Timestamp
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Key
    CONSTRAINT `FK_PaymentLog_Payment` FOREIGN KEY (`PaymentId`) REFERENCES `payments`(`Id`) ON DELETE CASCADE,
    
    -- Indexes
    INDEX `IDX_PaymentLog_PaymentId` (`PaymentId`),
    INDEX `IDX_PaymentLog_Action` (`Action`),
    INDEX `IDX_PaymentLog_CreatedAt` (`CreatedAt` DESC),
    INDEX `IDX_PaymentLog_PaymentId_CreatedAt` (`PaymentId`, `CreatedAt` DESC)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Payment activity audit log';

-- =============================================
-- Table: payment_metadata
-- Extended payment information and custom fields
-- =============================================
CREATE TABLE IF NOT EXISTS `payment_metadata` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `PaymentId` INT NOT NULL,
    
    -- Custom Values (EPS API support)
    `ValueA` VARCHAR(500) NULL,
    `ValueB` VARCHAR(500) NULL,
    `ValueC` VARCHAR(500) NULL,
    `ValueD` VARCHAR(500) NULL,
    
    -- Shipping Information
    `ShipmentName` VARCHAR(255) NULL,
    `ShipmentAddress` VARCHAR(500) NULL,
    `ShipmentAddress2` VARCHAR(500) NULL,
    `ShipmentCity` VARCHAR(100) NULL,
    `ShipmentState` VARCHAR(100) NULL,
    `ShipmentPostcode` VARCHAR(20) NULL,
    `ShipmentCountry` VARCHAR(10) NULL,
    `ShippingMethod` VARCHAR(100) NULL,
    
    -- Product List JSON
    `ProductListJson` TEXT NULL COMMENT 'JSON array of products',
    
    -- EPS Response Data
    `EpsResponseJson` TEXT NULL COMMENT 'Full EPS verification response',
    
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Key
    CONSTRAINT `FK_PaymentMetadata_Payment` FOREIGN KEY (`PaymentId`) REFERENCES `payments`(`Id`) ON DELETE CASCADE,
    
    -- Indexes
    UNIQUE INDEX `UNQ_PaymentMetadata_PaymentId` (`PaymentId`)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Extended payment metadata';

-- =============================================
-- Indexes for Analytics and Reporting
-- =============================================

-- Get all payments for a user with pagination
-- SELECT * FROM payments WHERE UserId = ? ORDER BY CreatedAt DESC LIMIT ? OFFSET ?

-- Get payment statistics by status
-- SELECT Status, COUNT(*) as Count, SUM(Amount) as Total FROM payments WHERE UserId = ? GROUP BY Status

-- Get recent failed payments for monitoring
-- SELECT * FROM payments WHERE Status = 'Failed' AND CreatedAt > DATE_SUB(NOW(), INTERVAL 1 DAY)

-- Find duplicate/suspicious transactions
-- SELECT MerchantTransactionId, COUNT(*) FROM payments GROUP BY MerchantTransactionId HAVING COUNT(*) > 1

-- =============================================
-- Sample Queries for Common Operations
-- =============================================

/*
-- Get payment with full details
SELECT p.*, pm.*, u.Email as UserEmail
FROM payments p
LEFT JOIN payment_metadata pm ON p.Id = pm.PaymentId
LEFT JOIN users u ON p.UserId = u.Id
WHERE p.MerchantTransactionId = ?;

-- Get payment history for user
SELECT Id, CustomerOrderId, MerchantTransactionId, Amount, Status, CreatedAt, CompletedAt
FROM payments
WHERE UserId = ?
ORDER BY CreatedAt DESC
LIMIT 20;

-- Get payment logs with full audit trail
SELECT pl.*
FROM payment_logs pl
WHERE pl.PaymentId = ?
ORDER BY pl.CreatedAt ASC;

-- Get payment statistics
SELECT 
    COUNT(*) as TotalTransactions,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as PendingCount,
    SUM(CASE WHEN Status = 'Success' THEN Amount ELSE 0 END) as TotalRevenue
FROM payments
WHERE UserId = ?;
*/

-- =============================================
-- Data Retention & Cleanup
-- =============================================

/*
-- Archive old completed payments (older than 1 year) - Run periodically
-- CREATE TABLE payments_archive LIKE payments;
-- INSERT INTO payments_archive SELECT * FROM payments WHERE Status = 'Success' AND CompletedAt < DATE_SUB(NOW(), INTERVAL 1 YEAR);
-- DELETE FROM payments WHERE Status = 'Success' AND CompletedAt < DATE_SUB(NOW(), INTERVAL 1 YEAR);

-- Clean up expired pending payments (older than 24 hours)
UPDATE payments 
SET Status = 'Expired', UpdatedAt = NOW() 
WHERE Status = 'Pending' AND CreatedAt < DATE_SUB(NOW(), INTERVAL 24 HOUR);
*/

-- =============================================
-- Security Best Practices
-- =============================================

/*
1. Store sensitive data encrypted (implement in application layer)
2. Use VerificationHash to prevent tampering
3. Log all payment activities for audit trail
4. Implement rate limiting in application layer
5. Monitor for suspicious patterns (multiple failed attempts, etc.)
6. Regular backup of payment data
7. Use prepared statements (Dapper handles this)
8. Validate all input data before insertion
*/

-- =============================================
-- End of Schema
-- =============================================
