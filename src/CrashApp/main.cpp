int main()
{
	int *bad_ptr = (int *)0xB0DEB0DE;

	*bad_ptr = 0x12345678;
	
	return 0;
}
