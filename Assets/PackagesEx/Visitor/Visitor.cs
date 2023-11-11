using System.Collections.Generic;

namespace Ugol.Visitor
{
    public interface IVisitable<in TVisitor>
    {
        void AcceptVisitor(TVisitor visitor);
    }

    public interface IVisitor<in TVisitable>
    {
        void Visit(TVisitable visitable);
    }

    public class VisitableBase<TConcreteVisitable> : IVisitable<IVisitor<TConcreteVisitable>> where TConcreteVisitable : VisitableBase<TConcreteVisitable>
    {
        public void AcceptVisitor(IVisitor<TConcreteVisitable> visitor)
        {
            visitor.Visit((TConcreteVisitable)this);
        }
    }
    
    public class MyVisitor : IVisitor<MyA>, IVisitor<MyB>
    {
        public void Visit(MyA visitable)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(MyB visitable)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MyA : IVisitable<IVisitor<MyA>>
    {
        public void AcceptVisitor(IVisitor<MyA> visitor)
        {
            visitor.Visit(this);
        }
    }

    public class MyB : IVisitable<IVisitor<MyB>>
    {
        public void AcceptVisitor(IVisitor<MyB> visitor)
        {
            visitor.Visit(this);
        }
    }

    public static class Test
    {
        public static void T1()
        {
            List<IVisitable<MyVisitor>> list = new() { new MyA(), new MyB() };

            MyVisitor myVisitor = new MyVisitor();
            foreach (IVisitable<MyVisitor> visitable in list)
            {
                visitable.AcceptVisitor(myVisitor);
            }
        }
    }
}