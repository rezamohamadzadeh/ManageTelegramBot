using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DAL.Migrations
{
    public partial class Initialize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tb_UserInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreateDateTime = table.Column<DateTime>(nullable: false),
                    ChatId = table.Column<long>(nullable: false),
                    UserName = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    LoginState = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tb_UserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tb_UserActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreateDateTime = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    UserInfoId = table.Column<Guid>(nullable: false),
                    Tb_UserInfoId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tb_UserActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tb_UserActivities_Tb_UserInfos_Tb_UserInfoId",
                        column: x => x.Tb_UserInfoId,
                        principalTable: "Tb_UserInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tb_UserActivities_Tb_UserInfoId",
                table: "Tb_UserActivities",
                column: "Tb_UserInfoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tb_UserActivities");

            migrationBuilder.DropTable(
                name: "Tb_UserInfos");
        }
    }
}
