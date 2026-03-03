from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
import os

# Using SQLite for easier deployment and local testing in sandbox
# In production, this can be changed back to MSSQL or another database
SQLALCHEMY_DATABASE_URL = "sqlite:///./school_schedule_v3.db"

engine = create_engine(
    SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
