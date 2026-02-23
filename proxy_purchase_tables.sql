CREATE TABLE IF NOT EXISTS `ProxyPurchase` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `Amount` decimal(20,2) NOT NULL,
  `Balance` decimal(20,2) NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `idx_username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ProxyPurchaseLog` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) NOT NULL,
  `Amount` decimal(20,2) NOT NULL,
  `Balance` decimal(20,2) NOT NULL,
  `ErrorMessage` text DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `idx_username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
