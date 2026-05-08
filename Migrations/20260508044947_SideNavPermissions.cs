using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SideNavPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "side_nav_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    path = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    permission = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_side_nav_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_side_nav_items_side_nav_items_parent_id",
                        column: x => x.parent_id,
                        principalTable: "side_nav_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_side_nav_items_parent_id_display_order",
                table: "side_nav_items",
                columns: new[] { "parent_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_side_nav_items_slug",
                table: "side_nav_items",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "side_nav_items");
        }
    }
}
