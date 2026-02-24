from fastapi import FastAPI, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List, Optional
from app.core.database import get_db
from app.models.database import Subject, Teacher, AcademicClass, Classroom, Lesson, User
from app.schemas import schemas

app = FastAPI(title="School Schedule API", description="API for managing school schedules and resources")

@app.get("/")
def read_root():
    return {"message": "Welcome to School Schedule API"}

# Subjects
@app.get("/subjects/", response_model=List[schemas.Subject])
def read_subjects(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    subjects = db.query(Subject).offset(skip).limit(limit).all()
    return subjects

@app.post("/subjects/", response_model=schemas.Subject)
def create_subject(subject: schemas.SubjectCreate, db: Session = Depends(get_db)):
    db_subject = Subject(**subject.model_dump())
    db.add(db_subject)
    db.commit()
    db.refresh(db_subject)
    return db_subject

# Teachers
@app.get("/teachers/", response_model=List[schemas.Teacher])
def read_teachers(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    teachers = db.query(Teacher).offset(skip).limit(limit).all()
    return teachers

@app.post("/teachers/", response_model=schemas.Teacher)
def create_teacher(teacher: schemas.TeacherCreate, db: Session = Depends(get_db)):
    db_teacher = Teacher(**teacher.model_dump())
    db.add(db_teacher)
    db.commit()
    db.refresh(db_teacher)
    return db_teacher

# Academic Classes
@app.get("/classes/", response_model=List[schemas.AcademicClass])
def read_classes(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    classes = db.query(AcademicClass).offset(skip).limit(limit).all()
    return classes

@app.post("/classes/", response_model=schemas.AcademicClass)
def create_class(academic_class: schemas.AcademicClassCreate, db: Session = Depends(get_db)):
    db_class = AcademicClass(**academic_class.model_dump())
    db.add(db_class)
    db.commit()
    db.refresh(db_class)
    return db_class

# Classrooms
@app.get("/classrooms/", response_model=List[schemas.Classroom])
def read_classrooms(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    classrooms = db.query(Classroom).offset(skip).limit(limit).all()
    return classrooms

# Lessons (Schedule)
@app.get("/lessons/", response_model=List[schemas.Lesson])
def read_lessons(class_id: Optional[int] = None, teacher_id: Optional[int] = None, db: Session = Depends(get_db)):
    query = db.query(Lesson)
    if class_id:
        query = query.filter(Lesson.AcademicClassId == class_id)
    if teacher_id:
        query = query.filter(Lesson.TeacherId == teacher_id)
    return query.all()

@app.post("/lessons/", response_model=schemas.Lesson)
def create_lesson(lesson: schemas.LessonCreate, db: Session = Depends(get_db)):
    db_lesson = Lesson(**lesson.model_dump())
    db.add(db_lesson)
    db.commit()
    db.refresh(db_lesson)
    return db_lesson

# Users
@app.get("/users/", response_model=List[schemas.User])
def read_users(db: Session = Depends(get_db)):
    return db.query(User).all()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
