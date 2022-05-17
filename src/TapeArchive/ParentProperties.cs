namespace TapeArchive;

using System;

public class ParentProperties
{
    private int? mode;
    private int? userId;
    private int? groupId;
    private DateTime? modificationTime;

    public int? Mode
    {
        get => this.mode;
        set
        {
            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.mode = value;
        }
    }

    public int? UserId
    {
        get => this.userId;
        set
        {
            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.userId = value;
        }
    }

    public int? GroupId
    {
        get => this.groupId;
        set
        {
            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.groupId = value;
        }
    }

    public DateTime? ModificationTime
    {
        get => this.modificationTime;
        set
        {
            if (value.HasValue && value.Value < DateTime.UnixEpoch)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.modificationTime = value;
        }
    }
}
