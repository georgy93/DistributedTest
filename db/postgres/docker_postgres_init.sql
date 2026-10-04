CREATE TABLE IF NOT EXISTS "TestData"
(
    "Id" BIGSERIAL PRIMARY KEY,
    "Value" TEXT NOT NULL,
    "Number" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO "TestData" ("Value", "Number")
VALUES ('Initial value', 1);