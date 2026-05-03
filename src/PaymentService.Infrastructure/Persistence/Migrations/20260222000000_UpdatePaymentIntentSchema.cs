using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaymentService.Infrastructure.Persistence.Migrations;

public partial class UpdatePaymentIntentSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Amount: decimal/numeric → bigint (cents).
        // Existing rows with fractional amounts are multiplied by 100 during conversion.
        migrationBuilder.Sql(
            "ALTER TABLE payment_intents " +
            "ALTER COLUMN amount TYPE bigint USING (amount * 100)::bigint;");

        migrationBuilder.AddColumn<string>(
            name: "provider_ref",
            table: "payment_intents",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "payment_intents",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        // Replace non-unique index with unique constraint
        migrationBuilder.DropIndex(
            name: "ix_payment_intents_booking_id",
            table: "payment_intents");

        migrationBuilder.CreateIndex(
            name: "ix_payment_intents_booking_id",
            table: "payment_intents",
            column: "booking_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_payment_intents_booking_id",
            table: "payment_intents");

        migrationBuilder.CreateIndex(
            name: "ix_payment_intents_booking_id",
            table: "payment_intents",
            column: "booking_id");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "payment_intents");

        migrationBuilder.DropColumn(
            name: "provider_ref",
            table: "payment_intents");

        migrationBuilder.Sql(
            "ALTER TABLE payment_intents " +
            "ALTER COLUMN amount TYPE numeric USING (amount / 100.0)::numeric;");
    }
}
