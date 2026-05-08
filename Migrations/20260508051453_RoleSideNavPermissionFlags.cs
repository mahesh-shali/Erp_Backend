using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RoleSideNavPermissionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_side_nav_permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roleid = table.Column<int>(type: "integer", nullable: false),
                    side_nav_item_id = table.Column<int>(type: "integer", nullable: false),
                    can_read = table.Column<bool>(type: "boolean", nullable: false),
                    can_write = table.Column<bool>(type: "boolean", nullable: false),
                    can_update = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_side_nav_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_side_nav_permissions_roles_roleid",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_side_nav_permissions_side_nav_items_side_nav_item_id",
                        column: x => x.side_nav_item_id,
                        principalTable: "side_nav_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_side_nav_permissions_roleid_side_nav_item_id",
                table: "role_side_nav_permissions",
                columns: new[] { "roleid", "side_nav_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_side_nav_permissions_side_nav_item_id",
                table: "role_side_nav_permissions",
                column: "side_nav_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_side_nav_permissions");
        }
    }
}
