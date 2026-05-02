CREATE TABLE `storage_items` (
  `storageItemId` int(11) NOT NULL AUTO_INCREMENT,
  `accountId` bigint(20) NOT NULL,
  `classId` int(11) NOT NULL,
  `amount` int(11) NOT NULL,
  `refine` tinyint(4) NOT NULL DEFAULT 0,
  `identified` tinyint(4) NOT NULL DEFAULT 1,
  `damaged` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`storageItemId`),
  KEY `accountId` (`accountId`),
  CONSTRAINT `storage_items_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `accounts` (`accountId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
