from fastapi import FastAPI, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List, Optional
from app.core.database import get_db
from app.models.database import Subject, Teacher, AcademicClass, Classroom, Lesson, User, Workload
from app.schemas import schemas

from app.core.database import engine
from app.models.database import Base

Base.metadata.create_all(bind=engine)

app = FastAPI(title="School Schedule API", description="API for managing school schedules and resources")

@app.get("/")
def read_root():
    return {"message": "Welcome to School Schedule API"}

# Auth
@app.post("/login", response_model=schemas.User)
def login(login_data: schemas.UserLogin, db: Session = Depends(get_db)):
    user = db.query(User).filter(User.Username == login_data.Username).first()
    if not user or user.Password != login_data.Password:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect username or password",
        )
    return user

@app.post("/register", response_model=schemas.User)
def register(user: schemas.UserCreate, db: Session = Depends(get_db)):
    db_user = db.query(User).filter(User.Username == user.Username).first()
    if db_user:
        raise HTTPException(status_code=400, detail="Username already registered")
    
    new_user = User(**user.model_dump())
    db.add(new_user)
    db.commit()
    db.refresh(new_user)
    return new_user

# Subjects
@app.get("/subjects/", response_model=List[schemas.Subject])
def read_subjects(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return db.query(Subject).offset(skip).limit(limit).all()

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
    return db.query(Teacher).offset(skip).limit(limit).all()

@app.get("/teachers/{teacher_id}", response_model=schemas.Teacher)
def read_teacher(teacher_id: int, db: Session = Depends(get_db)):
    teacher = db.query(Teacher).filter(Teacher.Id == teacher_id).first()
    if not teacher:
        raise HTTPException(status_code=404, detail="Teacher not found")
    return teacher

@app.post("/teachers/", response_model=schemas.Teacher)
def create_teacher(teacher: schemas.TeacherCreate, db: Session = Depends(get_db)):
    db_teacher = Teacher(**teacher.model_dump())
    db.add(db_teacher)
    db.commit()
    db.refresh(db_teacher)
    return db_teacher

@app.put("/teachers/{teacher_id}", response_model=schemas.Teacher)
def update_teacher(teacher_id: int, teacher: schemas.TeacherCreate, db: Session = Depends(get_db)):
    db_teacher = db.query(Teacher).filter(Teacher.Id == teacher_id).first()
    if not db_teacher:
        raise HTTPException(status_code=404, detail="Teacher not found")
    for key, value in teacher.model_dump().items():
        setattr(db_teacher, key, value)
    db.commit()
    db.refresh(db_teacher)
    return db_teacher

@app.delete("/teachers/{teacher_id}")
def delete_teacher(teacher_id: int, db: Session = Depends(get_db)):
    db_teacher = db.query(Teacher).filter(Teacher.Id == teacher_id).first()
    if not db_teacher:
        raise HTTPException(status_code=404, detail="Teacher not found")
    db.delete(db_teacher)
    db.commit()
    return {"message": "Teacher deleted"}

# Academic Classes
@app.get("/classes/", response_model=List[schemas.AcademicClass])
def read_classes(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return db.query(AcademicClass).offset(skip).limit(limit).all()

@app.post("/classes/", response_model=schemas.AcademicClass)
def create_class(academic_class: schemas.AcademicClassCreate, db: Session = Depends(get_db)):
    db_class = AcademicClass(**academic_class.model_dump())
    db.add(db_class)
    db.commit()
    db.refresh(db_class)
    return db_class

@app.put("/classes/{class_id}", response_model=schemas.AcademicClass)
def update_class(class_id: int, academic_class: schemas.AcademicClassCreate, db: Session = Depends(get_db)):
    db_class = db.query(AcademicClass).filter(AcademicClass.Id == class_id).first()
    if not db_class:
        raise HTTPException(status_code=404, detail="Class not found")
    for key, value in academic_class.model_dump().items():
        setattr(db_class, key, value)
    db.commit()
    db.refresh(db_class)
    return db_class

@app.delete("/classes/{class_id}")
def delete_class(class_id: int, db: Session = Depends(get_db)):
    db_class = db.query(AcademicClass).filter(AcademicClass.Id == class_id).first()
    if not db_class:
        raise HTTPException(status_code=404, detail="Class not found")
    db.delete(db_class)
    db.commit()
    return {"message": "Class deleted"}

# Classrooms
@app.get("/classrooms/", response_model=List[schemas.Classroom])
def read_classrooms(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return db.query(Classroom).offset(skip).limit(limit).all()

@app.post("/classrooms/", response_model=schemas.Classroom)
def create_classroom(classroom: schemas.ClassroomCreate, db: Session = Depends(get_db)):
    db_room = Classroom(**classroom.model_dump())
    db.add(db_room)
    db.commit()
    db.refresh(db_room)
    return db_room

# Workloads
@app.get("/workloads/", response_model=List[schemas.Workload])
def read_workloads(db: Session = Depends(get_db)):
    return db.query(Workload).all()

@app.post("/workloads/", response_model=schemas.Workload)
def create_workload(workload: schemas.WorkloadCreate, db: Session = Depends(get_db)):
    db_workload = Workload(**workload.model_dump())
    db.add(db_workload)
    db.commit()
    db.refresh(db_workload)
    return db_workload

@app.put("/workloads/{workload_id}", response_model=schemas.Workload)
def update_workload(workload_id: int, workload: schemas.WorkloadCreate, db: Session = Depends(get_db)):
    db_workload = db.query(Workload).filter(Workload.Id == workload_id).first()
    if not db_workload:
        raise HTTPException(status_code=404, detail="Workload not found")
    for key, value in workload.model_dump().items():
        setattr(db_workload, key, value)
    db.commit()
    db.refresh(db_workload)
    return db_workload

@app.delete("/workloads/{workload_id}")
def delete_workload(workload_id: int, db: Session = Depends(get_db)):
    db_workload = db.query(Workload).filter(Workload.Id == workload_id).first()
    if not db_workload:
        raise HTTPException(status_code=404, detail="Workload not found")
    db.delete(db_workload)
    db.commit()
    return {"message": "Workload deleted"}

# Lessons (Schedule)
@app.get("/lessons/", response_model=List[schemas.Lesson])
def read_lessons(
    class_id: Optional[int] = None, 
    teacher_id: Optional[int] = None, 
    day_of_week: Optional[int] = None,
    subject_id: Optional[int] = None,
    db: Session = Depends(get_db)
):
    query = db.query(Lesson)
    if class_id:
        query = query.filter(Lesson.AcademicClassId == class_id)
    if teacher_id:
        query = query.filter(Lesson.TeacherId == teacher_id)
    if day_of_week:
        query = query.filter(Lesson.DayOfWeek == day_of_week)
    if subject_id:
        query = query.filter(Lesson.SubjectId == subject_id)
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
