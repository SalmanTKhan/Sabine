-- Auction persistence (Slice G v1, text-mode). One row per active or
-- resolved auction. Item snapshot follows the same single-attachment
-- shape used by `mail_messages` so payouts can hand the same fields
-- back to MailManager.Send. The auction loop polls the table every
-- 30s and resolves any row whose `deadline` has passed.
--
-- `status` values:
--   0 = Open       (accepting bids)
--   1 = Sold       (resolved, winner paid out, seller paid out)
--   2 = Expired    (no bids, item returned to seller via mail)
--   3 = Cancelled  (seller cancelled before any bid; item returned)

CREATE TABLE `auctions` (
	`auctionId` int(11) NOT NULL AUTO_INCREMENT,
	`sellerId` int(11) NOT NULL,
	`sellerName` varchar(24) NOT NULL,
	`itemClassId` int(11) NOT NULL,
	`itemAmount` smallint(6) NOT NULL DEFAULT 1,
	`itemRefine` tinyint(4) NOT NULL DEFAULT 0,
	`itemIdentified` tinyint(1) NOT NULL DEFAULT 1,
	`itemDamaged` tinyint(1) NOT NULL DEFAULT 0,
	`startingPrice` int(11) NOT NULL,
	`buyoutPrice` int(11) NOT NULL DEFAULT 0,
	`currentBid` int(11) NOT NULL DEFAULT 0,
	`bidderId` int(11) NOT NULL DEFAULT 0,
	`bidderName` varchar(24) NOT NULL DEFAULT '',
	`status` tinyint(4) NOT NULL DEFAULT 0,
	`createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	`deadline` datetime NOT NULL,
	PRIMARY KEY (`auctionId`),
	KEY `idx_status_deadline` (`status`, `deadline`),
	KEY `idx_seller` (`sellerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
