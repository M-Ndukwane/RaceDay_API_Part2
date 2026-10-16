using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.API.Migrations
{
    /// <inheritdoc />
    public partial class BaselineExistingDatabase : Migration
    {
        /// <inheritdoc />
       
protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline only.
            // The RaceDay tables already exist in the database.
            // Do not create or modify existing tables here.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty to protect the existing database.
            // Rolling back this baseline will not drop existing tables.
        }
    }
}
