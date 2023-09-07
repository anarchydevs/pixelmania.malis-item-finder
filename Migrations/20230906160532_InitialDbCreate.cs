using Microsoft.EntityFrameworkCore.Migrations;

namespace ItemSqliteDb.Migrations
{
    public partial class InitialDbCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemContainers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContainerInstance = table.Column<int>(nullable: false),
                    Root = table.Column<int>(nullable: false),
                    CharacterInventoryId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemContainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemContainers_Inventories_CharacterInventoryId",
                        column: x => x.CharacterInventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Slots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SlotInstance = table.Column<int>(nullable: false),
                    ItemContainerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Slots_ItemContainers_ItemContainerId",
                        column: x => x.ItemContainerId,
                        principalTable: "ItemContainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemInfos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    LowInstance = table.Column<int>(nullable: false),
                    HighInstance = table.Column<int>(nullable: false),
                    Ql = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Instance = table.Column<int>(nullable: false),
                    SlotId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInfos_Slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemContainers_CharacterInventoryId",
                table: "ItemContainers",
                column: "CharacterInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInfos_SlotId",
                table: "ItemInfos",
                column: "SlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Slots_ItemContainerId",
                table: "Slots",
                column: "ItemContainerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemInfos");

            migrationBuilder.DropTable(
                name: "Slots");

            migrationBuilder.DropTable(
                name: "ItemContainers");

            migrationBuilder.DropTable(
                name: "Inventories");
        }
    }
}
