using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTrack.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImagePathInStorage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAssets_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Store = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSources_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Notify = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlists_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawDataJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_ProductSources_ProductSourceId",
                        column: x => x.ProductSourceId,
                        principalTable: "ProductSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_ProductId_ProductSourceId_CollectedAt",
                table: "PriceSnapshots",
                columns: new[] { "ProductId", "ProductSourceId", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_ProductSourceId",
                table: "PriceSnapshots",
                column: "ProductSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAssets_ProductId_Type",
                table: "ProductAssets",
                columns: new[] { "ProductId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Url",
                table: "Products",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSources_ProductId_Store_ExternalId",
                table: "ProductSources",
                columns: new[] { "ProductId", "Store", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ProductId",
                table: "Watchlists",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId_ProductId",
                table: "Watchlists",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "ProductAssets");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropTable(
                name: "ProductSources");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
