from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
import os

# Database connection string for SQL Server
# Note: You might need to change the server address for production
SQLALCHEMY_DATABASE_URL = "mssql+pyodbc://(localdb)\\MSSQLLocalDB/School11_Schedule_DB?driver=ODBC+Driver+17+for+SQL+Server&trusted_connection=yes"

engine = create_engine(SQLALCHEMY_DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
