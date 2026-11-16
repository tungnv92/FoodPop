CREATE TABLE `level_revisions` (
	`level_id` text NOT NULL,
	`version` integer NOT NULL,
	`payload` text NOT NULL,
	`name` text NOT NULL,
	`saved_at` text NOT NULL,
	`author_id` text NOT NULL,
	`author_email` text NOT NULL,
	`author_name` text NOT NULL,
	`note` text NOT NULL,
	`operation_id` text NOT NULL,
	PRIMARY KEY(`level_id`, `version`)
);
--> statement-breakpoint
CREATE UNIQUE INDEX `revision_operation` ON `level_revisions` (`operation_id`);--> statement-breakpoint
CREATE TABLE `shared_levels` (
	`id` text PRIMARY KEY NOT NULL,
	`name` text NOT NULL,
	`version` integer NOT NULL,
	`payload` text NOT NULL,
	`saved_at` text NOT NULL,
	`author_id` text NOT NULL,
	`author_email` text NOT NULL,
	`author_name` text NOT NULL,
	`operation_id` text NOT NULL
);
