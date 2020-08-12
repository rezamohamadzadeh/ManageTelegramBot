using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DAL.Migrations
{
    public partial class Initializses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tb_UserActivities_Tb_UserInfos_Tb_UserInfoId",
                table: "Tb_UserActivities");

            migrationBuilder.DropIndex(
                name: "IX_Tb_UserActivities_Tb_UserInfoId",
                table: "Tb_UserActivities");

            migrationBuilder.DropColumn(
                name: "Tb_UserInfoId",
                table: "Tb_UserActivities");

            migrationBuilder.CreateIndex(
                name: "IX_Tb_UserActivities_UserInfoId",
                table: "Tb_UserActivities",
                column: "UserInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tb_UserActivities_Tb_UserInfos_UserInfoId",
                table: "Tb_UserActivities",
                column: "UserInfoId",
                principalTable: "Tb_UserInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tb_UserActivities_Tb_UserInfos_UserInfoId",
                table: "Tb_UserActivities");

            migrationBuilder.DropIndex(
                name: "IX_Tb_UserActivities_UserInfoId",
                table: "Tb_UserActivities");

            migrationBuilder.AddColumn<Guid>(
                name: "Tb_UserInfoId",
                table: "Tb_UserActivities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tb_UserActivities_Tb_UserInfoId",
                table: "Tb_UserActivities",
                column: "Tb_UserInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tb_UserActivities_Tb_UserInfos_Tb_UserInfoId",
                table: "Tb_UserActivities",
                column: "Tb_UserInfoId",
                principalTable: "Tb_UserInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
