using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtemisBankingPro.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class SyncDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RealizadoPorUserId",
                table: "Transacciones",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministradorId",
                table: "Prestamos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "CuotasAmortizacion",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_RealizadoPorUserId",
                table: "Transacciones",
                column: "RealizadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_AdministradorId",
                table: "Prestamos",
                column: "AdministradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_AspNetUsers_AdministradorId",
                table: "Prestamos",
                column: "AdministradorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacciones_AspNetUsers_RealizadoPorUserId",
                table: "Transacciones",
                column: "RealizadoPorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_AspNetUsers_AdministradorId",
                table: "Prestamos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transacciones_AspNetUsers_RealizadoPorUserId",
                table: "Transacciones");

            migrationBuilder.DropIndex(
                name: "IX_Transacciones_RealizadoPorUserId",
                table: "Transacciones");

            migrationBuilder.DropIndex(
                name: "IX_Prestamos_AdministradorId",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "RealizadoPorUserId",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "AdministradorId",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "CuotasAmortizacion");
        }
    }
}
